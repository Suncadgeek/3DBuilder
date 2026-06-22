# Playbook de refacto — 3DBuilder (transposé de SKBuilder)

> Note de passation rédigée depuis la session de refacto de **SKBuilder** (ex-SKB2).
> Objectif : refaire pour 3DBuilder le même type de refonte, en réutilisant les décisions,
> l'architecture et les pièges déjà résolus. **Lis ce fichier en entier avant de commencer.**

---

## 0. Démarrer la nouvelle session du bon pied

- Lance Claude **dans ce dossier** (`C:\Users\pinty\source\repos\3DBuilder`) pour que git, les
  chemins et la mémoire projet soient isolés de SKBuilder.
- Le projet de référence (déjà refactoré, qui marche, à imiter) est à
  `C:\Users\pinty\source\repos\SKB2` (côté WSL : `/mnt/c/Users/pinty/source/repos/SKB2`).
  Tu peux le lire pour copier les patterns (csproj, NamingService, NxContext, AssemblyResolve…).
- Travaille sur une branche dédiée : `git checkout -b Refacto`.

---

## 1. Ce que fait 3DBuilder (périmètre métier)

3DBuilder est le **complément** de SKBuilder dans la chaîne SOLEIL II :

| Étape | Add-in | Rôle |
|---|---|---|
| 1 | **SKBuilder** | Lit l'Excel de maille → crée les **squelettes** (lignes/arcs + CSYS) et la **hiérarchie d'assemblage vide** (Anneau→Cellule→Arc/SD→Squelette + Ensemble Aimants). |
| 2 | **3DBuilder** | Ouvre un assemblage anneau **existant** (depuis le PDM, `@DB/<id>`), ouvre les **vrais modèles 3D d'aimants**, et les **importe comme composants** positionnés/contraints sur les **CSYS du squelette**. |

Autrement dit : SKBuilder pose le squelette nommé, 3DBuilder y accroche la géométrie réelle des aimants.
La cohérence des **noms de CSYS** entre les deux est donc critique (voir `docs/STRUCTURE_ANNEAU.md`
dans SKB2 : familles d'aimants par préfixe 2 lettres, suffixes `.NN`, `_X/Y`, `_1/2`, drift…).

### Routines existantes à porter (`3DBProgram.vb`, ~49 Ko, encodé UTF-16)
- `Main()` — point d'entrée, affiche le form.
- `Outline()` — **orchestrateur** : vérifie qu'aucune pièce n'est ouverte, ouvre l'assemblage anneau
  `@DB/<TextBox2>`, appelle `Open_Parts`, importe les aimants, gère la ProgressBar.
- `Open_Parts(storagering_part)` — ouvre toutes les pièces aimants nécessaires (retourne `CAOcomp(200,2)`).
- `Import_Magnets(CAOcomp, SkelComp, AssemblyPart)` — ajoute les aimants comme composants dans l'assemblage.
- `SetConstraints(SkelComp, CurrentMagComp, AssemblyPart, SkelCSYSName)` — contraint l'aimant sur le CSYS du squelette.
- `rename_instances()` — renommage des instances de composants.
- `reportComponentChildren(comp, …)` — parcours récursif de l'arbre d'assemblage.
- `GetUnloadOption()` — déchargement (à porter en `Immediately`).
- Config : fichier **plat** `Documents\3DBuilder_config.ini` (2 lignes : chemin + id PDM). À remplacer par JSON.

### Points faibles connus (mêmes maux que SKB2, à corriger)
- `Option Strict Off`, tableaux fixes (`CAOcomp(200,2)`), état global de `Module Module1`
  (`theSession`, `ufs`, `workPart`, `displayPart`, `referenceSet1`, `storagering_part`).
- **Excel Interop COM** (`Microsoft.Office.Interop.Excel`) → exige Excel installé, lent, process fantômes.
  **Remplacer par ClosedXML** comme dans SKBuilder.
- Aucune validation amont, pas de rollback, plante sur données/contexte invalides.
- UI WinForms mélangée à la logique NXOpen (tout dans `Button2_Click`/`Outline`).
- Chemins en dur dans le code (`C:\Users\pinty.000\Desktop\NX OUT\…` en commentaires).
- Mode **managé/PDM** câblé en dur (`@DB/`) sans détection natif/managé.

---

## 2. Architecture cible (identique à SKBuilder, éprouvée)

Solution C#, **.NET Framework 4.8**, WinForms, SDK-style, 3 projets + tests :

```
3DBuilder.sln
├─ src/3DBuilder.Core/      ← PUR : modèle, lecture Excel (ClosedXML), nommage, planning,
│                              config JSON, validation. AUCUNE dépendance NXOpen ni WinForms. Testable.
├─ src/3DBuilder.Nx/        ← Adapter NXOpen : ouverture de pièces, import de composants,
│                              contraintes, parcours d'assemblage, détection natif/managé, undo marks.
├─ src/3DBuilder/           ← Add-in chargé par NX : Program (Main + GetUnloadOption),
│                              GenerationService (orchestrateur), UI WinForms.
└─ tests/3DBuilder.Core.Tests/  ← xUnit, logique pure sans NX.
```

Principe directeur : **toute la logique métier dans Core (testable), tout NXOpen isolé dans Nx, l'UI ne
fait que remplir une config et appeler `GenerationService.Run(config, log)`.** Zéro logique dans les
handlers d'événements.

### Fichiers à copier/adapter depuis SKB2 (gain de temps immédiat)
- `Directory.Build.props` — définit `NxManagedDir` (défaut `C:\Apps\Siemens\NX2406\NXBIN\managed`,
  surchargeable par var d'env `NX_MANAGED_DIR`), `LangVersion latest`.
- `src/SKBuilder/SKBuilder.csproj` — modèle de csproj add-in : `OutputType=Library`, `net48`,
  `UseWindowsForms`, références NXOpen avec **`<Private>false</Private>`** (NE PAS copier les DLL NX :
  elles sont résolues depuis le NX de l'utilisateur → portabilité).
- `src/SKBuilder/Program.cs` — **le `AssemblyResolve` est CRITIQUE, recopie-le** (voir §3).
- `NxContext.cs` (remplace l'état global du module), `NxEnvironment.cs` (détection natif/managé),
  `NamingService.cs`, `ConfigStore.cs`, `ValidationResult.cs`, `ExcelLatticeReader.cs`.

---

## 3. Pièges NXOpen déjà résolus (NE PAS les redécouvrir)

1. **`System.Memory` FileLoadException sous NX** — NX (`ugraf.exe`) charge ses propres `System.*`
   et n'applique pas tes binding redirects. **Solution obligatoire** : dans le `static` ctor de
   `Program`, ajouter `AppDomain.CurrentDomain.AssemblyResolve += ResolveFromAddinFolder`, qui
   charge chaque dépendance (ClosedXML, System.Text.Json, System.Memory…) depuis le dossier de
   l'add-in **en ignorant la version demandée**. Copie la méthode telle quelle depuis SKB2 `Program.cs`.

2. **Sous-namespaces NXOpen invisibles dans les signatures** — `using NXOpen;` n'expose pas
   `NXOpen.Features`/`Assemblies`/`Positioning` en position de type de membre → CS0246. **Solution** :
   alias de namespace en tête de fichier, ex. `using Features = NXOpen.Features;`,
   `using Assemblies = NXOpen.Assemblies;`, `using Positioning = NXOpen.Positioning;`.

3. **`DatumCsysBuilder.Commit()` renvoie `NXObject`** → cast explicite vers `Features.Feature` (CS0266).

4. **`SelectionMode` ambigu** (collision NXOpen vs ton enum) → alias `using SelectionMode = ...Core.Model.SelectionMode;`.

5. **Détection natif/managé** : `Session.GetSession().PdmSession.GetTcserverSettings(out connectString, out discriminator)`
   → managé si `connectString` non vide ; fallback sur variables d'env (`UGII_UGMGR`/`UGII_TC_INSTALL_DIR`).
   Exposer un **override UI** (Auto / Natif / Managé). ⚠ 3DBuilder est aujourd'hui **100 % managé** (`@DB/`),
   donc le cas managé est le chemin nominal ici.

6. **Rollback** : encadrer la génération par un undo mark NX
   (`Session.SetUndoMark(MarkVisibility.Visible, "...")`) + `try/catch` qui rapporte proprement et
   laisse l'utilisateur annuler les pièces partielles.

---

## 4. Build & déploiement (procédure validée)

Build depuis WSL avec le dotnet Windows (le projet cible net48) :
```bash
"/mnt/c/Program Files/dotnet/dotnet.exe" build src/3DBuilder/3DBuilder.csproj -c Release
```
Sortie : `src/3DBuilder/bin/Release/net48/` = `3DBuilder.dll` + ses dépendances (ClosedXML & co).
**L'add-in n'est PAS un seul DLL** : il faut distribuer TOUT le dossier (le `AssemblyResolve` charge
les dépendances voisines). Les DLL NXOpen n'y sont pas (résolues depuis le NX de l'utilisateur).

Déploiement réseau (même cible que SKBuilder) — WSL ne monte pas le lecteur V:, passer par PowerShell :
```bash
powershell.exe -NoProfile -Command "Copy-Item 'C:\Users\pinty\source\repos\3DBuilder\src\3DBuilder\bin\Release\net48\*' -Destination 'V:\INFORMATIQUE\PLM-Nx\Macros NXOpen\3DBuilder' -Recurse -Force -Exclude '*.pdb'"
```
(Créer le dossier cible `…\Macros NXOpen\3DBuilder` d'abord. Déployer quand personne n'utilise
l'add-in : NX verrouille les DLL pendant l'exécution.)
Chargement dans NX : **Fichier ▸ Exécuter ▸ NX Open** → pointer `…\3DBuilder\3DBuilder.dll`.

> ⚠ Identité git : utiliser auteur **Victor Pinty `<victor.pinty@synchrotron-soleil.fr>`**.
> Sur `/mnt/c`, l'écriture de `.git/config` peut échouer → passer par les variables d'env
> `GIT_AUTHOR_NAME/EMAIL` + `GIT_COMMITTER_NAME/EMAIL` à chaque commit.

---

## 5. Plan de refonte suggéré (ordre)

1. Branche `Refacto` ; créer la solution C# 3 projets + tests ; copier `Directory.Build.props`,
   les csproj modèles, `Program.cs` (avec AssemblyResolve), `NxContext`, `ConfigStore`, `NamingService`.
2. **Core** : modèle (composant aimant, mapping de CSYS cible, options), `ExcelLatticeReader`
   (ClosedXML, réutilisable depuis SKB2), validation, config JSON (`3DBuilder_config.json` en
   remplacement du `.ini`). Tests unitaires sur le parsing/nommage/mapping.
3. **Nx** : porter `Open_Parts`, `Import_Magnets`, `SetConstraints`, `reportComponentChildren`,
   `rename_instances` en services isolés (`NxAssemblyService`, `NxPartFactory`), + undo marks +
   détection environnement.
4. **App** : `Program` (Main/GetUnloadOption), `GenerationService` (orchestrateur unique), UI WinForms
   repensée (chemin Excel, id assemblage PDM / mode natif-managé, options, progression, journal d'erreurs).
5. Build Release, déployer sur V:, tester dans NX2406 sur un assemblage anneau réel, comparer à l'ancien.
6. Supprimer le code VB une fois le mode managé validé.

---

## 6. Cohérence avec SKBuilder (à vérifier)

Les **noms de CSYS** produits par SKBuilder (`NamingService` + `LatticeComputer` dans SKB2) sont les
points d'accroche de 3DBuilder. Avant de coder `SetConstraints`, relire dans SKB2 :
`docs/STRUCTURE_ANNEAU.md`, `src/SKBuilder.Core/Geometry/NamingService.cs` et `LatticeComputer.cs`
pour reproduire **exactement** la convention de nommage (sinon les contraintes ne trouveront pas leurs CSYS).
Idéalement, factoriser un jour le nommage commun, mais dans un premier temps : **dupliquer fidèlement**.
