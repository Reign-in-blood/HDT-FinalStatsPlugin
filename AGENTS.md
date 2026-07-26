# AGENTS.md — FinalStatsPlugin

## 1. Rôle de ce fichier

Ce fichier définit les règles permanentes de développement de **FinalStatsPlugin**.

Il s’applique à l’ensemble du dépôt, sauf si un dossier contient plus tard un fichier `AGENTS.md` plus spécifique.

Avant toute modification :

1. lire ce fichier entièrement ;
2. inspecter les fichiers concernés ;
3. comprendre le comportement actuel ;
4. proposer ou effectuer une modification ciblée ;
5. compiler et vérifier le résultat lorsque l’environnement le permet.

Les instructions explicites données par l’utilisateur pour une tâche précise restent prioritaires.

---

## 2. Présentation du projet

**FinalStatsPlugin** est un plugin Windows pour **Hearthstone Deck Tracker (HDT)**, destiné au mode **Hearthstone Battlegrounds**.

Nom affiché dans HDT :

```text
Battlegrounds Final Stats
```

Objectifs actuels :

- suivre des statistiques pendant une partie de Battlegrounds ;
- afficher ces statistiques dans un panneau WPF sur l’overlay HDT ;
- permettre de masquer le panneau pendant la partie ;
- forcer l’affichage du résumé final après le retour au menu ;
- conserver un fonctionnement local, léger et lisible ;
- ne jamais perturber Hearthstone ou HDT en cas d’erreur du plugin.

Orientation future prévue :

- enregistrer un historique structuré des parties et des combats ;
- stocker les données localement en JSON ;
- fournir un tableau de bord local en HTML, CSS et JavaScript ;
- ouvrir ce tableau de bord depuis le menu Plugins de HDT ;
- ne pas utiliser d’application compagnon `.exe` ;
- ne pas exiger de serveur local ;
- ne pas transmettre de données sur Internet.

---

## 3. Profil de l’utilisateur et communication

L’utilisateur principal est francophone et débutant en programmation C#/.NET.

Lors des comptes rendus :

- répondre en français ;
- expliquer concrètement ce qui a été modifié ;
- éviter le jargon inutile ;
- ne pas supposer que l’utilisateur connaît Git, Visual Studio, MSBuild ou WPF ;
- fournir les commandes exactes lorsqu’une action manuelle est nécessaire ;
- distinguer clairement :
  - ce qui a été modifié ;
  - ce qui a été compilé ;
  - ce qui a été testé ;
  - ce qui reste à vérifier en partie ;
- ne jamais prétendre qu’une compilation ou un test a réussi s’il n’a pas réellement été exécuté ;
- signaler honnêtement les limites de l’environnement.

Ne pas donner uniquement des fragments de code à copier lorsque la tâche demande une modification complète du projet. Modifier les fichiers du dépôt de manière cohérente.

---

## 4. Structure actuelle du dépôt

Structure de référence :

```text
FinalStatsPlugin.sln
FinalStatsPlugin.csproj
FinalStatsPlugin.cs
Build.bat
find_hdt_assembly.ps1
LISEZ-MOI.txt
.gitignore
lib/
dist/
```

Rôle des fichiers :

### `FinalStatsPlugin.cs`

Contient actuellement :

- l’implémentation `IPlugin` ;
- le cycle de vie HDT ;
- le suivi de partie ;
- les compteurs ;
- l’analyse de `Power.log` ;
- la détection des transitions d’entités ;
- le calcul des dégâts de héros ;
- la création et la mise à jour de l’overlay WPF ;
- le bouton Show/Hide ;
- les diagnostics.

### `FinalStatsPlugin.csproj`

Configuration actuelle :

```text
TargetFramework: net472
OutputType: Library
UseWPF: true
PlatformTarget: x64
LangVersion: 10
```

Références externes :

```text
lib/HearthstoneDeckTracker.exe
lib/HearthDb.dll
```

Ces deux fichiers sont des dépendances locales de compilation et ne doivent pas être publiés dans le dépôt.

### `Build.bat`

Script de compilation principal sous Windows.

Il :

1. reçoit le dossier HDT ou le chemin de `HearthstoneDeckTracker.exe` ;
2. utilise `find_hdt_assembly.ps1` ;
3. copie les véritables assemblies HDT dans `lib/` ;
4. cherche MSBuild ;
5. compile en `Release|x64` ;
6. copie `FinalStatsPlugin.dll` dans `dist/`.

### `find_hdt_assembly.ps1`

Recherche récursivement un véritable assembly .NET nommé :

```text
HearthstoneDeckTracker.exe
```

Il doit éviter de sélectionner un simple lanceur non managé.

### `LISEZ-MOI.txt`

Documentation française de la version de développement.

Elle doit être mise à jour lorsqu’un comportement, une statistique, une limitation ou une procédure change.

---

## 5. Environnement technique

Contraintes obligatoires :

- Windows ;
- .NET Framework 4.7.2 ;
- WPF ;
- architecture x64 ;
- C# 10 ;
- compatibilité avec la version HDT actuellement ciblée ;
- aucune dépendance NuGet supplémentaire sans justification explicite ;
- aucune dépendance réseau pour le fonctionnement normal du plugin.

Ne pas migrer vers .NET moderne, WinUI, Avalonia, Electron ou une application séparée sans demande explicite.

Ne pas modifier l’installation HDT de l’utilisateur.

Ne jamais inscrire dans le dépôt :

- des chemins personnels absolus ;
- des noms de profil Windows ;
- des tokens ;
- des identifiants privés ;
- les DLL de HDT ;
- les fichiers générés de compilation ;
- des logs personnels.

---

## 6. Commandes de compilation

Commande recommandée depuis la racine du projet :

```bat
Build.bat "CHEMIN_VERS_LE_DOSSIER_HDT"
```

Ou :

```bat
Build.bat "CHEMIN_VERS_HearthstoneDeckTracker.exe"
```

Exécution non interactive possible :

```bat
cmd /c Build.bat "CHEMIN_VERS_LE_DOSSIER_HDT"
```

Résultat attendu :

```text
dist\FinalStatsPlugin.dll
```

En cas d’échec :

1. lire toute la sortie de MSBuild ;
2. corriger la première erreur réelle ;
3. relancer la compilation ;
4. ne pas masquer les erreurs par des contournements génériques ;
5. ne pas déclarer la tâche terminée tant que l’état réel n’est pas expliqué.

Avant de compiler, vérifier que :

```text
lib\HearthstoneDeckTracker.exe
lib\HearthDb.dll
```

sont présents ou peuvent être récupérés par `Build.bat`.

Si l’environnement ne contient pas Windows, Visual Studio/MSBuild ou les assemblies HDT :

- effectuer uniquement les vérifications statiques possibles ;
- indiquer explicitement que la compilation HDT n’a pas été exécutée ;
- ne pas inventer un résultat de build.

---

## 7. Règles de modification du code

### 7.1 Modifications ciblées

Préférer les changements petits, isolés et vérifiables.

Ne jamais effectuer de remplacement global approximatif sur des noms de méthodes ou d’identifiants.

Éviter notamment les remplacements pouvant produire des erreurs comme :

```text
TryTryExtractEntityId
```

Avant toute modification :

- rechercher toutes les occurrences concernées ;
- vérifier les appels et la déclaration ;
- examiner le diff final.

### 7.2 Préserver les fonctions stables

Une tâche concernant un compteur ne doit pas modifier les autres compteurs sans nécessité démontrée.

Une tâche concernant l’esthétique ne doit pas modifier les calculs.

Une tâche concernant le stockage JSON ne doit pas modifier silencieusement les valeurs affichées dans l’overlay.

Ne pas effectuer de refactorisation générale opportuniste pendant une correction de bug.

### 7.3 Style C#

Conserver le style existant :

- indentation de 4 espaces ;
- accolades sur des lignes séparées ;
- noms de types et méthodes en `PascalCase` ;
- champs privés en `_camelCase` ;
- constantes explicites ;
- commentaires courts et utiles ;
- logique lisible plutôt que compacte ;
- utilisation de `CultureInfo.InvariantCulture` pour les données sérialisées ou numériques techniques ;
- pas de `dynamic` sauf nécessité absolue et documentée.

Les identifiants et commentaires techniques peuvent rester en anglais, conformément au code existant.

Les textes visibles dans l’overlay doivent rester cohérents avec l’interface actuelle en anglais.

### 7.4 Exceptions

Le plugin ne doit jamais faire planter HDT.

Pour les traitements exécutés fréquemment :

- intercepter les exceptions au niveau approprié ;
- journaliser une information exploitable ;
- éviter les blocs `catch` silencieux, sauf dans la fonction de diagnostic elle-même ;
- ne jamais lancer volontairement une exception dans `OnUpdate()`.

---

## 8. Cycle de vie HDT

Événements actuellement utilisés :

```text
GameEvents.OnGameStart
GameEvents.OnGameEnd
GameEvents.OnInMenu
GameEvents.OnEntityWillTakeDamage
```

Méthodes principales :

```text
OnLoad
OnUnload
OnButtonPress
OnUpdate
HandleGameStart
HandleGameEnd
HandleInMenu
BeginMatch
FinishMatch
ResetStatistics
TrackMatch
```

Règles :

- enregistrer les événements une seule fois lors du chargement ;
- ne pas ajouter plusieurs abonnements pour le même traitement ;
- tenir compte du fait que `OnUpdate()` est appelé environ toutes les 100 ms ;
- éviter les accès disque lourds ou les analyses complètes inutiles à chaque `OnUpdate()` ;
- maintenir un état explicite pour éviter les doubles initialisations et doubles finalisations ;
- rendre les opérations de fin de partie idempotentes ;
- ne pas effacer les statistiques finales avant le début réel de la partie suivante.

États importants actuels :

```text
_trackingMatch
_hasMatchData
_gameEndObserved
_showingFinalSummary
_newGameEventPending
_previousCombatPhase
```

Toute modification de ces états doit être examinée avec les scénarios suivants :

1. lancement normal d’une partie ;
2. début du premier recrutement ;
3. début et fin de plusieurs combats ;
4. égalité d’un combat ;
5. victoire d’un combat ;
6. défaite d’un combat ;
7. fin normale de partie ;
8. retour au menu ;
9. nouvelle partie ;
10. activation/désactivation du plugin ;
11. reconnexion éventuelle.

---

## 9. Overlay WPF

Dimensions et position de référence :

```text
PanelWidth: 250
PanelHeight: 780
PanelRight: 15
PanelBottom: 50
ToggleButtonHeight: 30
ToggleButtonGap: 6
```

Comportement attendu :

### Pendant une partie

- le bouton est visible ;
- le bouton affiche :
  - `Hide combat stats` si le panneau est visible ;
  - `Show combat stats` si le panneau est masqué ;
- le choix de visibilité est conservé pendant la partie.

### Après la partie, dans le menu

- le panneau final est forcé visible ;
- le bouton est masqué ;
- la zone interactive du bouton est retirée ;
- les statistiques restent visibles jusqu’à la prochaine partie.

### Partie suivante

- le bouton réapparaît ;
- la préférence précédente de visibilité peut être restaurée.

Règles WPF/HDT :

- créer et modifier les contrôles via le `Dispatcher` de `Core.OverlayCanvas` ;
- ne pas accéder directement aux contrôles WPF depuis un thread non UI ;
- ne pas recréer inutilement tout l’overlay à chaque mise à jour ;
- garder le panneau non interactif ;
- enregistrer uniquement le bouton comme zone interactive avec :

```csharp
OverlayExtensions.SetIsOverlayHitTestVisible(element, true);
```

- désenregistrer la zone interactive quand elle est masquée ou supprimée ;
- ne jamais rendre tout l’overlay HDT cliquable ;
- ne pas bloquer les clics destinés à Hearthstone ;
- préserver les états visuels normal, survolé et pressé du bouton.

---

## 10. Sémantique des statistiques actuelles

Statistiques suivies :

```text
Highest turn
Gold spent
Tavern rolls
Free rolls gained
Battlecries played
Rally triggered
Cards bought
Minions bought
Spells bought
Played cards
Played minions
Played spells
Highest creature
Highest ATK
Highest HP
Tavern buff max
Spell power buff
Hero damage dealt
Max damage dealt
Hero damage taken
Max damage taken
```

Les noms, l’ordre et la signification ne doivent pas changer sans demande explicite.

### 10.1 Gold spent

Méthode de référence :

- observer `RESOURCES_USED` ;
- ajouter uniquement les augmentations positives ;
- une diminution sert uniquement de nouvelle ligne de base ;
- une vente ne doit jamais être interprétée comme une dépense ;
- un changement de tour ne doit pas ajouter artificiellement la valeur courante.

Ne pas réintroduire aveuglément `NUM_RESOURCES_SPENT_THIS_GAME` sans preuve qu’il est fiable en Battlegrounds pour la version HDT/Hearthstone ciblée.

### 10.2 Cartes achetées

La détection actuelle :

- mémorise les identifiants d’entités visibles dans la Taverne ;
- détecte le passage de la même entité vers la main du joueur ;
- classe l’entité comme serviteur ou sort de Taverne ;
- empêche le double comptage par identifiant.

Limite connue :

- une carte obtenue gratuitement directement depuis la Taverne peut être comptée comme achetée.

Scénario obligatoire de test :

- achat de la troisième copie d’un serviteur créant immédiatement un triple.

### 10.3 Dégâts des héros

Source actuelle :

```text
GameEvents.OnEntityWillTakeDamage
PREDAMAGE
```

Filtrage obligatoire :

- partie Battlegrounds active ;
- phase de combat active ;
- cible de type héros ;
- identifiant exact correspondant au tag `HERO_ENTITY` de `PlayerEntity` ou `OpponentEntity`.

Raison :

- Battlegrounds peut exposer plusieurs entités ressemblant à des héros ;
- un même impact peut produire plusieurs notifications ;
- les copies de classement ou d’affichage ne doivent pas être comptées.

Comportement actuel :

- conserver la plus grande valeur `PREDAMAGE` reçue par chaque héros pendant un combat ;
- finaliser une seule fois à la fin du combat ;
- un draw doit produire `0` dégât infligé et `0` dégât reçu ;
- une disparition ou réinitialisation d’armure ne doit jamais être comptée comme dégât ;
- les dégâts absorbés par l’armure doivent rester inclus dans la valeur réelle de l’impact.

Ne pas revenir à une simple différence :

```text
DAMAGE + perte d’ARMOR
```

Cette ancienne méthode a déjà créé des valeurs erronées, notamment l’ajout de toute l’armure adverse lors d’un draw.

### 10.4 Tavern buff max

Le compteur dépend actuellement des données exposées par HDT.

Le compteur intégré HDT a déjà cessé de s’afficher après certains patchs Hearthstone.

Règles :

- ne pas inventer une valeur ;
- ne pas additionner tous les tags génériques `TAG_SCRIPT_DATA_NUM_1/2` ;
- ces tags sont utilisés par de nombreuses entités sans rapport avec le buff de Taverne ;
- si le compteur officiel reste vide, ajouter d’abord un diagnostic ciblé ;
- identifier précisément :
  - le `CardId` ;
  - l’entité ;
  - le contrôleur ;
  - les tags ;
  - la phase de jeu ;
- valider en partie avant d’activer une nouvelle méthode de calcul.

### 10.5 Power.log

Le plugin analyse certaines lignes de `Core.Game.PowerLog`.

Règles :

- ne traiter que les nouvelles lignes avec `_processedPowerLogLines` ;
- ne pas reparcourir tout le log à chaque mise à jour ;
- conserver les protections contre les lignes incomplètes ;
- limiter les regex au strict nécessaire ;
- éviter les regex catastrophiques ;
- utiliser les identifiants d’entités lorsque cela est possible ;
- ne pas supposer qu’un nom de carte localisé sera stable ;
- préférer les `CardId`, tags et contrôleurs.

---

## 11. Diagnostics

Fichier actuel :

```text
FinalStatsPlugin_debug.log
```

Il est écrit à côté de la DLL chargée.

Règles :

- les diagnostics ne doivent jamais interrompre HDT ;
- `WriteDiagnostic()` doit rester protégé par un `try/catch` final ;
- chaque nouvelle détection fragile doit disposer de messages exploitables ;
- éviter d’écrire la même ligne toutes les 100 ms ;
- journaliser les transitions, décisions et valeurs, pas seulement « erreur » ;
- ne pas inclure de données privées inutiles ;
- ne pas publier les logs dans Git.

Pour un bug intermittent, préférer temporairement des lignes structurées :

```text
EVENT | key=value | key=value
```

Exemple :

```text
HERO PREDAMAGE DEALT | target=42 | value=8 | combatMax=8
```

Une fois le problème stabilisé, réduire les diagnostics excessifs.

---

## 12. Versionnement

La version du plugin se trouve dans :

```csharp
public Version Version => new Version(MAJOR, MINOR, PATCH);
```

Règles :

- incrémenter la version pour chaque version distribuée ou testée ;
- une correction ou petite fonctionnalité augmente généralement `PATCH` ;
- ne pas modifier rétroactivement une archive déjà distribuée ;
- mettre à jour `LISEZ-MOI.txt` avec :
  - la nouvelle version ;
  - les changements ;
  - les limitations ;
  - les tests attendus ;
- vérifier que le nom du ZIP, le texte de documentation et la propriété `Version` correspondent.

Ne pas incrémenter la version pour une simple analyse sans changement de fichiers.

---

## 13. Git et GitHub

Avant modification :

```bash
git status
git branch --show-current
```

Après modification :

```bash
git diff --check
git diff
git status
```

Règles :

- ne jamais travailler directement sur une branche publiée sans vérifier la consigne de l’utilisateur ;
- préférer une branche dédiée pour les fonctionnalités importantes ;
- ne pas forcer un push ;
- ne pas réécrire l’historique ;
- ne pas utiliser `git reset --hard` ;
- ne pas supprimer des fichiers non suivis sans autorisation ;
- ne pas modifier ou supprimer des changements locaux de l’utilisateur ;
- ne pas committer automatiquement sauf demande explicite ;
- ne pas pousser ni ouvrir de pull request sans demande explicite ;
- utiliser des commits petits et descriptifs.

Exemples de messages :

```text
fix: prevent duplicate hero damage counting
feat: add local match history storage
feat: add static statistics dashboard
docs: update build and testing instructions
```

Ne pas committer :

```text
bin/
obj/
dist/*.dll
lib/HearthstoneDeckTracker.exe
lib/HearthDb.dll
*.log
.vs/
```

Respecter `.gitignore`.

---

## 14. Vérifications obligatoires avant livraison

Pour chaque modification C# :

1. vérifier le diff ;
2. vérifier les accolades et la structure ;
3. vérifier les noms de méthodes et appels ;
4. vérifier qu’aucun remplacement accidentel n’a été introduit ;
5. compiler avec `Build.bat` si l’environnement le permet ;
6. confirmer la création de :

```text
dist\FinalStatsPlugin.dll
```

7. vérifier que seuls les fichiers attendus ont changé ;
8. mettre à jour la version et la documentation si une version est distribuée.

Pour une modification d’overlay :

- panneau visible ;
- panneau masqué ;
- bouton cliquable ;
- bouton non cliquable lorsqu’il est caché ;
- clics du jeu non bloqués ;
- retour au menu ;
- nouvelle partie ;
- plusieurs résolutions d’écran si possible.

Pour une modification de compteur :

- première occurrence ;
- plusieurs occurrences ;
- zéro occurrence ;
- changement de tour ;
- draw ;
- victoire ;
- défaite ;
- vente d’un serviteur ;
- triple immédiat si pertinent ;
- fin de partie ;
- nouvelle partie sans redémarrer HDT.

Si aucun test en jeu n’est possible, fournir une liste précise des scénarios que l’utilisateur doit vérifier.

---

## 15. Définition de « terminé »

Une tâche n’est terminée que lorsque :

- le comportement demandé est implémenté ;
- les fonctions stables n’ont pas été modifiées sans raison ;
- le diff est propre ;
- la compilation a réussi, ou son impossibilité est clairement signalée ;
- les tests exécutés sont listés ;
- les tests manuels restants sont listés ;
- la version et la documentation sont cohérentes si nécessaire ;
- aucun fichier généré ou privé n’a été ajouté ;
- aucun chemin personnel n’a été inscrit ;
- le compte rendu final est compréhensible par un débutant.

---

## 16. Future fonctionnalité : historique local des parties

L’architecture prévue doit rester locale et transparente.

### 16.1 Principes

- aucune application compagnon `.exe` ;
- aucun service Windows ;
- aucun serveur HTTP local par défaut ;
- aucun port réseau ;
- aucune télémétrie ;
- aucun envoi vers un service distant ;
- fonctionnement hors ligne ;
- fichiers lisibles et exportables.

### 16.2 Stockage canonique

Utiliser JSON comme source de vérité.

Structure recommandée :

```text
Dashboard/
├── index.html
├── style.css
├── app.js
├── data.js
└── Data/
    ├── games-index.json
    └── games/
        ├── game-YYYY-MM-DD-HH-mm-ss-ID.json
        └── ...
```

Chaque fichier de partie doit contenir au minimum :

```text
schemaVersion
pluginVersion
gameId
startedAt
endedAt
heroCardId
heroName
placement
turnCount
duration
finalStats
combats
```

Chaque combat peut contenir :

```text
turn
opponent
result
damageDealt
damageTaken
tavernTier
turnStats
playerBoard
opponentBoard
```

Ne pas enregistrer une donnée comme certaine si HDT ne permet pas de la déterminer fiablement.

Utiliser des valeurs explicites comme :

```text
unknown
null
```

plutôt qu’une valeur inventée.

### 16.3 Compatibilité du schéma

Tous les fichiers JSON doivent contenir :

```json
{
  "schemaVersion": 1
}
```

Règles :

- ne pas casser silencieusement les anciens fichiers ;
- ajouter les nouveaux champs de façon compatible ;
- incrémenter `schemaVersion` lors d’un changement incompatible ;
- prévoir une lecture tolérante des champs manquants ;
- documenter les migrations.

### 16.4 Écriture sûre

Les sauvegardes de partie doivent être atomiques :

1. écrire dans un fichier temporaire ;
2. fermer le fichier ;
3. remplacer ou renommer vers le fichier final.

Ne pas laisser un JSON partiellement écrit si HDT ferme brutalement.

Les erreurs d’écriture :

- doivent être journalisées ;
- ne doivent pas interrompre le plugin ;
- ne doivent pas bloquer `OnUpdate()`.

Éviter une écriture disque toutes les 100 ms.

Capturer les données en mémoire pendant la partie, puis sauvegarder aux transitions importantes ou à la fin.

### 16.5 Vie privée

Par défaut :

- les données restent sur l’ordinateur ;
- ne pas enregistrer de BattleTag complet si ce n’est pas nécessaire ;
- prévoir la possibilité de masquer ou anonymiser les noms adverses ;
- ne pas enregistrer de chemin personnel dans les fichiers ;
- ne charger aucun script distant.

---

## 17. Future fonctionnalité : dashboard HTML local

Technologies autorisées :

```text
HTML
CSS
JavaScript
JSON
```

Ne pas créer de fichier Java `.java`.

Ne pas créer d’application compagnon.

### 17.1 Chargement local

Une page ouverte avec `file://` peut être limitée pour les appels `fetch()` vers des fichiers locaux.

Architecture recommandée :

- conserver les fichiers `.json` comme source canonique ;
- générer également un fichier `data.js` ;
- exposer les données sous une variable globale contrôlée :

```javascript
window.FINAL_STATS_DATA = {
    schemaVersion: 1,
    games: []
};
```

Dans `index.html` :

```html
<script src="data.js"></script>
<script src="app.js"></script>
```

Ne pas dépendre d’un CDN.

Si une bibliothèque graphique est utilisée :

- la distribuer localement ;
- vérifier sa licence ;
- documenter son origine et sa version ;
- éviter une bibliothèque lourde si SVG ou Canvas natif suffit.

### 17.2 Ouverture depuis HDT

Utiliser le `MenuItem` fourni par `IPlugin` pour ajouter un menu clair, par exemple :

```text
Plugins
└── Battlegrounds Final Stats
    ├── Open statistics dashboard
    └── Open data folder
```

Lors de l’ouverture :

1. vérifier que les fichiers existent ;
2. générer ou actualiser `data.js` ;
3. ouvrir `index.html` avec le navigateur par défaut ;
4. gérer proprement l’absence du fichier ;
5. journaliser l’erreur sans faire planter HDT.

Ne pas détourner le bouton Show/Hide de l’overlay pour cette fonction si un menu dédié est disponible.

### 17.3 Dashboard initial minimal

Commencer par :

- nombre de parties ;
- placement moyen ;
- durée moyenne ;
- statistiques moyennes ;
- liste des dernières parties ;
- vue détaillée d’une partie ;
- graphique des dégâts infligés et reçus par tour ;
- graphique de l’or dépensé par tour.

Ajouter les fonctions avancées uniquement après validation de la collecte.

---

## 18. Stratégie de développement des nouvelles données

Pour toute nouvelle statistique :

1. définir précisément sa signification ;
2. identifier la source HDT ;
3. déterminer quand la valeur apparaît ;
4. vérifier les entités et contrôleurs ;
5. ajouter un diagnostic temporaire ;
6. tester sur plusieurs parties ;
7. tester les cas nuls et atypiques ;
8. seulement ensuite afficher ou sauvegarder la statistique.

Ne pas construire un graphique avant d’avoir validé la donnée source.

Pour une donnée par tour, conserver :

```text
valeur cumulée avant le tour
valeur cumulée après le tour
delta du tour
```

Pour une donnée par combat, conserver un objet de combat séparé et finalisé une seule fois.

Ne pas mélanger :

- statistique cumulative de partie ;
- statistique du tour ;
- statistique du combat ;
- maximum historique.

---

## 19. Refactorisation future

Le fichier principal est actuellement volumineux.

Une séparation progressive pourra être envisagée, sans refactorisation massive, par exemple :

```text
Plugin.cs
Tracking/MatchTracker.cs
Tracking/CombatTracker.cs
Tracking/PowerLogTracker.cs
Tracking/PurchaseTracker.cs
Models/MatchHistory.cs
Models/CombatHistory.cs
Storage/JsonHistoryStore.cs
Dashboard/DashboardGenerator.cs
Overlay/StatsOverlay.cs
Diagnostics/DiagnosticLogger.cs
```

Règles :

- ne pas déplacer tout le code en une seule tâche ;
- extraire une responsabilité à la fois ;
- maintenir le comportement identique ;
- compiler après chaque extraction ;
- éviter les changements de logique cachés dans une refactorisation ;
- ne pas introduire une architecture complexe sans bénéfice immédiat.

La future collecte JSON constitue un bon point de départ pour créer des modèles et un service de stockage séparés.

---

## 20. Priorités du projet

Ordre de priorité :

1. ne pas faire planter HDT ;
2. ne pas bloquer les interactions avec Hearthstone ;
3. exactitude des statistiques ;
4. absence de double comptage ;
5. conservation correcte des données entre les phases ;
6. compatibilité avec HDT ;
7. compilation reproductible ;
8. lisibilité du code ;
9. esthétique ;
10. fonctions avancées.

En cas de conflit, privilégier toujours la stabilité et l’exactitude plutôt qu’une fonctionnalité supplémentaire.
