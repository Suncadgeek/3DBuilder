# Mapping de refonte — ancien VB → architecture cible C#

> Compagnon de `REFACTO_PLAYBOOK.md`. Décrit, routine par routine, où va chaque morceau du
> `3DBProgram.vb` actuel dans la solution cible (Core / Nx / App), et les décisions de modèle prises.
> **À valider avant génération de code.**

---

## 0. Décisions actées (2026-06-22)

| # | Sujet | Décision |
|---|---|---|
| D1 | Format dictionnaire | **2 colonnes** : `A` = réf TC (`CAO000…`), `B` = code physique machine. **Pas de colonne révision.** Feuille `dico`. |
| D2 | Ouverture aimant | `@DB/<réf TC>` **sans révision** → NX charge la **dernière révision *working*** (latest working rule). |
| D3 | Lecture Excel | **ClosedXML** (plus de COM Interop ; supprime les 2 ouvertures + process fantômes). |
| D4 | Filtres CSYS | Centralisés dans **un seul** `NamingService` (aujourd'hui dupliqués/incohérents : `1/9…9/9` vs `1/5…5/5`). |
| D5 | Config | **JSON** `3DBuilder_config.json` (remplace le `.ini` plat 2 lignes). |
| D6 | Rollback | Génération encadrée par **undo mark** + try/catch métier. |
| D7 | Mode PDM | **Managé** nominal (`@DB/`), avec détection natif/managé + override UI (Auto/Natif/Managé). |
| D8 | Fractions | Règle unique **« fraction médiane `⌈b/2⌉/b` »** (2/3, 3/5, 5/9, 6/11). Remplace les listes `InStr` `/3,/5,/9` et **corrige le cas `/11`** non géré par le VB. |
| D9 | Matching code↔dico | **STRICT** (exact, sensible à la casse). Écart → warning non bloquant (aimant sauté + journal). Pas de normalisation auto. |
| D10 | OCT_* / QCORR_* | **Volontairement non importés** : certains `SXT_***` sont des doublets contenant le qpôle/octupôle ; on n'importe que le SXT via son RPM, ce qui place le 2ᵉ élément mécaniquement. Absence du dico = warning silencieux **attendu**. |
| D11 | Mode **Natif** (test) vs **Managé** (prod) | **Managé** (défaut, prod) : pièces résolues via `@DB/<réf TC>` (Teamcenter). **Natif** (test) : pièces résolues depuis un **dossier fichier-système** spécifié par l'utilisateur (aimants ET anneau/cellules). Override UI Auto/Natif/Managé (cf. D7). |
| D12 | CSYS aimant | Aligner sur le CSYS nommé **`RPM`** dans **les deux modes**. Le `template.prt` de test **contient un RPM** → pas d'override « 1ᵉʳ CSYS » nécessaire. |

## Convention de nommage décodée (export squelette réel, 2026-06-22)

```
<CODE>.NN                  groupe de features d'un aimant (NN = instance : .01, .02…)
  <CODE>.NN                DATUM_CSYS de montage (calque 1)  ← point d'accroche
  <CODE>.NN Entrée         CSYS de position (calque 60)      ← EXCLU
  <CODE>.NN Sortie         CSYS de position (calque 60)      ← EXCLU
DRIFT.NN                   espaceur                           ← EXCLU
<CODE>_a/b.NN              aimant long en b fractions → garder seulement la médiane ⌈b/2⌉/b
```
Extraction code : retirer `.NN`, puis le suffixe fraction `_a/b` → `CODE` matché contre colonne B du dico.
Le CSYS `RPM` est dans la **pièce aimant** (0 occurrence côté squelette) → contrainte RPM(aimant) ↔ CSYS médian (squelette).

---

## 0bis. Contrôle pré-génération (preflight) — OBLIGATOIRE avant toute écriture NX

Étape de vérification **lecture seule** exécutée avant la génération : parse les clés des deux côtés,
croise, et présente un rapport que l'utilisateur valide. **Aucune modification NX tant que le rapport
n'est pas confirmé.** (Justifié : sur un squelette réel, 8 codes/27 non couverts.)

**Pipeline** :
1. `NxAssemblyService` énumère les noms de features CSYS de tous les squelettes de l'anneau (Nx, lecture seule).
2. `NamingService.ExtractMagnetCode` extrait les codes (Core).
3. `MagnetDictionaryReader` charge le dico (Core).
4. `PreflightChecker.Check(skeletonCodes, dictionary)` produit un `PreflightReport` (Core, testable).

**`PreflightReport`** (sévérité → possibilité d'override) :
| Catégorie | Contenu | Sévérité | Override ? |
|---|---|---|---|
| `Matched` | codes squelette présents dans le dico (→ importés, avec n° d'occurrences) | info | — |
| `MissingFromDictionary` | codes squelette absents du dico (→ **sautés**, listés) | warning | ✅ oui |
| `KnownExcluded` | manquants reconnus comme volontaires (préfixes `OCT_`, `QCORR_` — cf. D10) | info | — |
| `MissingMagnetAssembly` | cellule **sans Ensemble Aimants** (précondition SKBuilder absente — cf. n°3) | warning | ✅ oui (cellule exclue du remplissage) |
| `ConventionDrift` | cellule avec squelettes mais **0 code matché** (convention probablement obsolète — cf. n°7) | warning fort | ✅ oui |
| `AmbiguousRpm` | pièce aimant avec **0 ou >1** CSYS `RPM` (cf. n°4) | warning | ✅ oui (aimant sauté) |
| `AlreadyPopulated` | cellule déjà (partiellement) remplie → mode incrémental ou force (cf. idempotence) | info | — |
| `UnusedDictionaryEntries` | entrées du dico jamais référencées | info | — |
| `DuplicateDictionaryKeys` | violation d'unicité du dico (D1) | **erreur** | ❌ **bloquant** |

**Modèle de sévérité** :
- `erreur` → **bloque** la génération, **non contournable**.
- `warning` → affiché dans le rapport ; l'utilisateur peut **override explicitement** (case à cocher
  « j'ai pris connaissance / continuer malgré tout ») ; l'élément concerné est sauté, pas l'ensemble.
- `info` → purement indicatif.

Le rapport est **systématique, obligatoire, présenté avant tout remplissage**, et **exportable** (journal
horodaté, cf. n°5) pour corriger le dictionnaire / les squelettes avant relance.

La liste des préfixes `KnownExcluded` est **configurable** (défaut `OCT_`, `QCORR_`) pour éviter de
re-signaler des absences attendues à chaque run.

### Scope / granularité (sélection de cellules)

L'UI doit permettre de générer **tout l'anneau OU une/plusieurs cellules** précises.
- `GenerationConfig.SelectedCells` : liste de noms de cellules. **Vide = tout l'anneau.**
- `NxAssemblyService.EnumerateCells()` (lecture seule) renvoie les `CellDescriptor` disponibles
  (niveau `RootComponent.GetChildren(j)` = niveau Cellule de la hiérarchie Anneau→Cellule→Arc/SD→Squelette).
- Le **preflight et la génération opèrent sur le scope choisi uniquement** (le rapport ne signale que
  les codes des cellules sélectionnées → pas de bruit sur le reste de l'anneau).

**Flux UI en 2 temps** (remplace le « tout en aveugle » actuel) :
1. **Analyser** : ouvre l'anneau `@DB/<TC>` (lecture seule), énumère les cellules, charge le dico,
   exécute le `PreflightChecker` → affiche la **liste des cellules cochables** + le **rapport preflight**.
2. **Générer** : sur les cellules cochées seulement, encadré par undo mark. Bouton actif uniquement si
   aucune erreur bloquante (les warnings doivent être override).

### Idempotence : incrémental par défaut + remplissage forcé (features n°1)

Une cellule peut être vide, à moitié remplie, ou pleine. Pour chaque cellule, le preflight détecte les
aimants **déjà présents** dans l'Ensemble Aimants (par comparaison composants posés ↔ CSYS attendus).

- **Mode incrémental (DÉFAUT)** : on **n'ajoute que les aimants manquants** ; les déjà-posés sont laissés
  intacts → re-run sûr, pas de doublon. Une cellule pleine et conforme = rien à faire (no-op).
- **Mode forcé (`ForceRefill`, opt-in par cellule)** : **purge d'abord les composants aimants** de
  l'Ensemble Aimants (le **squelette = 1ᵉʳ sous-produit est CONSERVÉ**), puis remplit à neuf. Exige une
  **confirmation utilisateur explicite** (dialogue distinct listant les composants à retirer).

`NxAssemblyService` :
- `GetPlacedMagnets(ensembleAimants)` → composants aimants déjà présents (pour le diff incrémental).
- `PurgeMagnets(ensembleAimants)` → retire les composants aimants en préservant le squelette (mode forcé).

### Vérification post-génération (feature n°2)

Après remplissage, `NxAssemblyService` + `PostGenerationVerifier` (Core) produisent un **bilan** :
attendus vs posés vs **échoués** (aimant non contraint, RPM introuvable, contrainte non résolue, DOF
restants). Affiché et journalisé. Un échec ne casse pas le run mais est **remonté explicitement**
(sinon « anneau qui a l'air bon mais faux »).

---

## 0ter. Mode Natif (test) vs Managé (prod) — D11/D12

L'outil tourne dans deux contextes, sélectionnables via override UI (Auto / Natif / Managé) :

| Aspect | **Managé** (prod) | **Natif** (test) |
|---|---|---|
| Détection | `PdmSession.GetTcserverSettings(...)` non vide (cf. playbook §3.5) | sinon / forcé via UI |
| Anneau / cellules | `@DB/<réf TC anneau>` | fichier/dossier `NativeRingPath` |
| Aimants | `@DB/<réf TC>` (latest working) | `<NativeMagnetsFolder>/<réf TC>.prt` |
| CSYS aimant pour contrainte | CSYS nommé **`RPM`** | CSYS nommé **`RPM`** (le template de test en a un) |

**Isolation** : la résolution de pièce est une interface (`IPartResolver`) à deux implémentations
(`ManagedPartResolver` → `@DB/…`, `NativePartResolver` → chemin fichier). Le reste du code
(preflight, diff, contraintes, parcours, CSYS RPM) est **identique** dans les deux modes.

**Fixture de test simulant la base d'aimants** (déjà en place, 2026-06-22) :
`~/Downloads/3DB_native_fixture/magnets/` contient **50 copies de `template.prt`** (fourni, avec un CSYS
`RPM`), une par réf TC du dictionnaire (`CAO000154690.prt`, …). Permet d'exécuter le mode natif sans
Teamcenter. Dossier **hors repo** (binaires non versionnés). `NativeMagnetsFolder` pointera dessus.

---

## 1. Modèle de données (Core, pur)

| Type | Rôle | Origine VB |
|---|---|---|
| `MagnetDictionary` (`IReadOnlyDictionary<string,string>` code→réfTC) | Dictionnaire chargé, unicité validée | `CAOitem`/`CAOcomp` tableaux `(200,2)` |
| `GenerationConfig` { `DictionaryExcelPath`, `StorageRingTcRef`, `PdmMode`, `SelectedCells`, `NativeMagnetsFolder`, `NativeRingPath` } | Entrées utilisateur (les 2 derniers : mode natif seulement) | `TextBox1`, `TextBox2`, `.ini` |
| `CellDescriptor` { `Name`, `Index`, `Sections[]` } | 1 cellule sélectionnable (énumérée depuis l'anneau ouvert) | boucle `j` de `Outline` |
| `MagnetPlacement` { `MagnetCode`, `TcRef`, `SkeletonCsysName` } | 1 aimant à poser sur 1 CSYS | calculé dans `Import_Magnets` |
| `ValidationResult` { `IsValid`, `Errors[]`, `Warnings[]` } | Validation amont | (inexistant) |

---

## 2. Découpe par routine

### `Open_Parts` (l.109-263)  →  **3 responsabilités séparées**
| Bloc VB | Cible | Service |
|---|---|---|
| Ouverture Excel + lecture cellules | **Core** | `MagnetDictionaryReader.Read(path)` (ClosedXML) |
| Parcours features CSYS du squelette + filtres `InStr` + extraction code | **Core** | `NamingService.ExtractMagnetCode(featureName)` + `IsMountingCsys(featureName)` |
| Croisement code↔dictionnaire + pré-ouverture `@DB/<TC>` | **Nx** | `NxPartFactory.OpenMagnetParts(codes, dict)` |

### `Outline` (l.34-107)  →  **App orchestrateur**
| Bloc VB | Cible | Service |
|---|---|---|
| Vérif « aucune pièce ouverte » | App/Nx | `GenerationService.Run` (garde) |
| Ouverture anneau `@DB/<TC>` | **Nx** | `NxAssemblyService.OpenStorageRing(tcRef)` |
| Navigation `GetChildren(j).GetChildren(k)…` figée | **Nx** | `NxAssemblyService.EnumerateSkeletonAssemblies()` (parcours robuste, pas d'indices en dur) |
| Boucle import + ProgressBar | **App** | `GenerationService` + `IProgress<…>` |
| `SetWorkComponent(Nothing)` retour anneau | **Nx** | `NxAssemblyService.SetWorkToRoot()` |
| `rename_instances()` | **Nx** | `NxRenameService.RenameInstances()` |

### `Import_Magnets` (l.265-419)  →  **Nx, sans relire l'Excel**
- Supprime la 2ᵉ ouverture Excel (le dictionnaire est déjà en mémoire — passé en paramètre).
- `NxAssemblyService.AddMagnet(skelComp, assemblyPart, magnetPart, refSet="MODEL")` (encapsule `AddComponentBuilder`).
- Cas spécial `DNL`/`DNC` → règle nommée dans `NamingService` (et **corrige** le bug `Or InStr(FeatName,"DNC")` sans `> 0`).
- Chaque pose → `NxConstraintService.Constrain(...)`.

### `SetConstraints` (l.423-493)  →  **Nx `NxConstraintService`**
- Aligne **CSYS squelette** ↔ **CSYS `RPM` aimant** (`Touch`/`InferAlign`, `SetFixHint`).
- Le filtre de sélection du CSYS squelette utilise le **même `NamingService`** que l'extraction (fin des listes `InStr` divergentes).
- `FindObject("PROTO#.Features|<ji>|CSYSTEM 1")` conservé tel quel (API NXOpen).

### `rename_instances` + `reportComponentChildren` (l.500-562)  →  **Nx `NxRenameService`**
- Parcours récursif → renomme via `ObjectGeneralPropertiesBuilder` (`child.Prototype.Name`).
- Le `ListingWindow` devient un `ILog` injecté (testable, découplé de NX).

### `GetUnloadOption` (l.494) + `Unload.vb`  →  **App `Program`**
- Une seule implémentation `Immediately`. Supprime le doublon `Unload.vb`.

### `Module1` état global (l.19-25)  →  **Nx `NxContext`**
- `theSession/ufs/workPart/displayPart/storagering_part` → propriétés d'instance, rafraîchies à la demande (plus de capture figée au chargement).

### `Form1` (`.vb` + `.Designer.vb`)  →  **App UI**
- Handlers vidés de toute logique → `GenerationService.Analyze(config)` puis `GenerationService.Run(config, progress, log)`.
- **Flux 2 temps** : bouton **Analyser** (preflight + énumération cellules, lecture seule) → bouton **Générer** (scope coché).
- **Sélection de cellules** : liste cochable (`CheckedListBox`) peuplée par `Analyze` ; case « Tout l'anneau » = `SelectedCells` vide.
- **Rapport preflight** affiché : Matched / MissingFromDictionary (warning) / erreurs bloquantes ; Générer désactivé si erreur bloquante.
- `.ini` Read/Write → `ConfigStore` JSON.
- Champs : chemin dictionnaire, réf TC anneau, mode PDM (Auto/Natif/Managé), sélection cellules, progression, journal.
- Nettoyer : `RootNamespace=SKB2`, défauts `C:\Users\pinty.000\…`, `RightToLeftLayout=True`.

---

## 3. Projets cibles (rappel playbook §2)

```
3DBuilder.Core/   MagnetDictionary, MagnetDictionaryReader (ClosedXML), NamingService,
                  GenerationConfig, ValidationResult, PreflightChecker, PreflightReport,
                  PostGenerationVerifier, CellDescriptor, ConfigStore (JSON)    ← testable, zéro NX
3DBuilder.Nx/     NxContext, NxEnvironment, NxAssemblyService (EnumerateCells, GetPlacedMagnets,
                  PurgeMagnets, AddMagnet, SetWorkToRoot), IPartResolver (Managed/Native — D11),
                  NxConstraintService (CSYS RPM), NxRenameService, undo marks,
                  IRunLog (journal horodaté)
3DBuilder/        Program (Main + GetUnloadOption + AssemblyResolve), GenerationService
                  (Analyze + Run + Cancel coopératif), UI (cellules cochables, rapport, override)
tests/…Core.Tests xUnit : reader, extraction code (DNL/DNC, suffixes .NN), fraction médiane (/3,/5,/9,/11),
                  PreflightChecker (couverture 19/27, OCT/QCORR exclus, drift), diff incrémental
```

### Récap features de robustesse v1 (n°1-7) → où elles vivent
| # | Feature | Emplacement |
|---|---|---|
| 1 | Idempotence (incrémental + force refill) | `PreflightChecker` (diff) + `NxAssemblyService.GetPlacedMagnets/PurgeMagnets` |
| 2 | Vérification post-génération | `PostGenerationVerifier` (Core) + relevé Nx |
| 3 | Précondition Ensemble Aimants absent | `PreflightChecker` → `MissingMagnetAssembly` (warning) |
| 4 | RPM ambigu (0 ou >1) | `NamingService`/`NxConstraintService` → `AmbiguousRpm` (warning) |
| 5 | Journal horodaté exportable | `IRunLog` (Nx/App) — preflight + résultat |
| 6 | Annulation coopérative | `GenerationService.Cancel` (flag testé entre 2 aimants, NX mono-thread STA) |
| 7 | Détection dérive de convention | `PreflightChecker` → `ConventionDrift` (warning fort) |

---

## 4. Tests Core prioritaires (logique aujourd'hui non testée)

1. `MagnetDictionaryReader` : lit 2 colonnes, **rejette les doublons** de code, ignore lignes vides.
2. `NamingService.ExtractMagnetCode` : suffixe `.NN`, cas `DNL/DNC` (découpe sur `_`), codes composés.
3. `NamingService.IsMountingCsys` : exclut `Entrée/Sortie/Drift/1-9/9…` ; inclut un vrai CSYS aimant.
4. Mapping code→réf TC : code absent du dictionnaire → **warning explicite** (vs échec silencieux actuel).
