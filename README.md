# 3DBuilder

Add-in **NXOpen** (Siemens NX 2406) pour la chaîne **SOLEIL II**. 3DBuilder est le **complément de SKBuilder** : là où SKBuilder pose le **squelette nommé** (lignes/arcs + repères CSYS) et la hiérarchie d'assemblage **vide** d'un anneau de stockage, 3DBuilder **accroche la géométrie réelle des aimants** sur ce squelette.

---

## Ce que fait 3DBuilder

À partir d'un **anneau de stockage existant** (vide d'aimants) et d'un **dictionnaire Excel** (code physique-machine → référence TC) :

1. ouvre l'anneau et parcourt ses **cellules** et leurs **Ensembles Aimants** (coquilles créées par SKBuilder) ;
2. lit les **CSYS de montage** du squelette de chaque ensemble pour savoir quels aimants poser et où ;
3. ouvre les **vrais modèles 3D d'aimants** (depuis Teamcenter ou un dossier) ;
4. les **importe comme composants** dans l'Ensemble Aimants (ensemble de référence `MODEL`) ;
5. les **contraint** en alignant le CSYS `RPM` de l'aimant sur le CSYS de montage du squelette ;
6. renomme les instances et produit un **bilan**.

Structure produite, par cellule :

```
Cellule (Arc / Section Droite)
└─ Ensemble Aimants (ex: ARC14_AIMANTS)        ← créé par SKBuilder
   ├─ Squelette (ex: ARC14_SQL)                ← 1er sous-produit (exclu du ref-set MODEL)
   └─ Aimant (ex: QUADRUPOLE … QF08)           ← ajouté par 3DBuilder, contraint sur le CSYS du squelette
```

---

## Le dictionnaire aimants (`.xlsx`)

Fichier Excel **à 2 colonnes, sans en-tête** (feuille `dico` par défaut, sinon la première feuille) :

| Colonne A | Colonne B |
|---|---|
| Référence TC (`CAO000NNNNNN`) | Code physique-machine (ex. `QUAD_6.48_21_126`, `SXT_6.2_16_120`) |

Contrainte : **un code = une référence TC** (les doublons de code sont signalés comme **erreur bloquante**). Un exemple est versionné : [`Dictionnaire Aimants.xlsx`](Dictionnaire%20Aimants.xlsx).

---

## Utilisation dans NX

1. **Charger l'add-in** : `Fichier ▸ Exécuter ▸ NX Open…` puis pointer `3DBuilder.dll` (charger **tout le dossier**, voir Déploiement).
2. **Renseigner** : chemin du dictionnaire, réf TC de l'anneau, mode (voir ci-dessous).
3. **① Analyser** — *lecture seule, aucune écriture* : ouvre l'anneau, énumère les cellules, croise squelettes ↔ dictionnaire et affiche un **rapport pré-génération** (preflight) + la **liste des cellules** cochables. À faire avant tout remplissage.
4. **② Générer** — sur les cellules cochées, sous *undo mark* : pose et contraint les aimants, puis affiche un **bilan** (posés / échecs / sautés). Bouton actif uniquement si le preflight n'a **aucune erreur bloquante**.

### Modes d'exécution

| Mode | Usage | Résolution des pièces |
|---|---|---|
| **Managé** | production (Teamcenter) | `@DB/<réf TC>` (dernière révision *working*) |
| **Natif** | test, sans Teamcenter | `<dossier>/<réf TC>.prt` (dossier aimants + fichier anneau à indiquer) |
| **Auto** | détection automatique | managé si session Teamcenter, sinon natif |

### Périmètre & remplissage

- **Tout l'anneau** ou une sélection de **cellules** (la sélection est conservée d'une analyse à l'autre ; boutons *Tout cocher / décocher*).
- **Incrémental** (défaut) : n'ajoute que les aimants **manquants** ; une cellule déjà complète n'est pas touchée → relances sûres, pas de doublon.
- **Remplissage forcé** (case à cocher, confirmation explicite) : **purge** d'abord les aimants présents (le squelette est conservé) puis repose à neuf.

### Rapport pré-génération (preflight)

Classé par sévérité, avec **journal filtrable** (Info / Avertissements / Erreurs) :

- **Erreur bloquante** : doublon de clé dans le dictionnaire → empêche la génération.
- **Avertissement** (non bloquant, aimant/cellule sauté) : code absent du dictionnaire, Ensemble Aimants manquant, dérive de convention de nommage, CSYS `RPM` absent/ambigu.
- **Info** : codes attendus volontairement absents (`OCT_`, `QCORR_` — intégrés mécaniquement dans certains sextupôles doublets), entrées du dictionnaire inutilisées, etc.

---

## Architecture (solution C#, .NET Framework 4.8)

```
3DBuilderCS.sln
├─ src/3DBuilder.Core/   Logique métier PURE, testable, sans NXOpen :
│                        nommage (NamingService), lecture dictionnaire (ClosedXML),
│                        preflight, config JSON, validation.
├─ src/3DBuilder.Nx/     Adapter NXOpen : ouverture anneau, parcours cellules /
│                        Ensembles Aimants, ajout / purge de composants, contraintes.
├─ src/3DBuilder/        Add-in chargé par NX : Program (Main + AssemblyResolve),
│                        GenerationService (orchestrateur), UI WinForms.
├─ src/3DBuilder.Preview/ Lanceur d'aperçu de l'UI hors NX (développement uniquement).
└─ tests/3DBuilder.Core.Tests/   xUnit (nommage, dictionnaire, preflight).
```

Principe : **toute la logique métier dans Core (testée)**, **tout NXOpen isolé dans Nx**, l'UI ne fait que remplir une config et appeler `GenerationService.Analyze` / `Run`.

### Convention de nommage (rappel)

Les CSYS de montage du squelette ont la forme `<CODE>.NN` (instance) ou `<CODE>_a/b.NN` (aimant long découpé — seule la **fraction médiane** `⌈b/2⌉/b` porte le montage). Les CSYS `Entrée`/`Sortie` et `DRIFT` sont ignorés. Le `<CODE>` est matché **à l'identique** (sensible à la casse) contre la colonne B du dictionnaire.

---

## Build

Depuis WSL avec le dotnet Windows (cible `net48`) :

```bash
"/mnt/c/Program Files/dotnet/dotnet.exe" build src/3DBuilder/3DBuilder.csproj -c Release
```

Sortie : `src/3DBuilder/bin/Release/net48/` = `3DBuilder.dll` + ses dépendances (ClosedXML, System.Text.Json…). **Les DLL NXOpen n'y sont pas** : elles sont résolues depuis le NX de l'utilisateur (référence `<Private>false</Private>`), ce qui assure la portabilité. Le `AssemblyResolve` du `Program` charge les dépendances voisines depuis le dossier de l'add-in.

Tests : `"/mnt/c/Program Files/dotnet/dotnet.exe" test 3DBuilderCS.sln`.

> Le chemin du NX managé est `C:\Apps\Siemens\NX2406\NXBIN\managed` (surchargeable par la variable d'environnement `NX_MANAGED_DIR`, voir `Directory.Build.props`).

---

## Déploiement

L'add-in **n'est pas un seul DLL** : il faut distribuer **tout le dossier** Release (sauf les `.pdb`). Cible réseau :

```
V:\INFORMATIQUE\PLM-Nx\Macros NXOpen\3DBuilder\
```

Déployer **quand personne n'utilise l'add-in** (NX verrouille les DLL pendant l'exécution). Chargement : `Fichier ▸ Exécuter ▸ NX Open` → `…\3DBuilder\3DBuilder.dll`.

---

## Configuration

Les saisies de l'UI sont mémorisées dans `Documents\3DBuilder_config.json` (chemin du dictionnaire, réf TC, mode, dossiers natifs…).

---

*Refonte C# du add-in VB d'origine. Détails de conception : `REFACTO_PLAYBOOK.md` et `REFACTO_MAPPING.md`.*
