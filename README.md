# Songho-Matrix
Ce projet est inspiré très fortement du site http://www.songho.ca/opengl et particulièrement du programme C++ : http://www.songho.ca/opengl/files/matrix.zip pour la partie dessin. 
Il permet d'avoir un aperçu de la mise en œuvre de la librairie OpentTK pour le Framework 4.8
Il se compose d'une partie commune qui concerne le dessin de la scène. Une GameWindow et 2 Forms avec un GLControl permettent d'afficher le dessin.
Le 1ère Forms émule les évenements UpdateFrame et RenderFrame absent du ControlGL à travers une minuterie ou l'évenement d'application Idle.
Le 2ème Forms implémente une boucle de jeux.
Les 3 rendus permettent d'avoir une animation du modèle manuelle ou automatique.

## Affichage de Texte
La dll OpenTK.Texte permet l'affichage de texte à partir de n'importe quelle police accessible sous windows. Le code VB correspondant est fourni. 
Le code initial C# provient d'une archive de la version N°1.1.4 d'OpenTK.Compatibility `class TextPrinter` et ces *dépendances* que l'on peut trouver ici : https://sourceforge.net/projects/opentk/
L'article suivant https://www.codeproject.com/Articles/1057539/Abstract-of-the-text-rendering-with-OpenGL-OpenTK permet d'approfondir ce sujet particulier.

## Actions possibles
- Touches communes aux 3 fenêtres
   - Passage en fenêtre maximisée et vice versa --> F11 
   - Rendu des triangles en Plein, Fil, Point -->  F
   - Epaisseur Fil et Point de 1 à 3 --> L
   - Arrêt des évenements UpdateFrame et RenderFrame et de l'animation automatique du modèle --> A
   - Bascule Animation du modèle (Rotation axe Y) en automatique ou animation en manuel --> R
   - Augmentation des FPS d'animation --> P ou Pavé numérique + 
   - Diminution des FPS d'animation --> M ou Pavé numérique -
   - Bouton Gauche de la souris appuyé --> déplacement de la caméra sur les 3 axes
   - Bouton Droit de la souris appuyé --> éloignement ou rapprochement du point de visée (zoom)
- Touche fenêtre GameWindows   
   - Prise en compte des évenements UpdateFrame et RenderFrame pour l'animation --> G
- Touches fenêtre GLControl N°1
   - Démarage des évenements UpdateFrame et RenderFrame avec l'émulation à partir de l'évenementd'application Idle --> I
   - Démarage des évenements UpdateFrame et RenderFrame avec l'émulation à partir de d'une minuterie --> T
- Touches fenêtre GLControl N°2
   - Démarage des évenements UpdateFrame et RenderFrame avec une boucle pour les 2 évenements --> B
   - Démarage des évenements UpdateFrame et RenderFrame avec une boucle pour chaque évenement --> Ctrl+B

![image](https://user-images.githubusercontent.com/81978881/114317360-045a4300-9b08-11eb-8be9-669bc93e583d.png)

## Particularité du code
Pour ce 1er programme la partie dessin est simple et n'utilise que le mode OpenGL immédiat. Le principal est de voir l'implentation d'un rendu OpenGL et de comprendre les différents aspects de celle-ci. N'hésiter pas à lire les commentaires et à faire varier les différents paramètres dans le code afin de voir l'impact sur les ressources de votre système. Le gestionnaire des tâches est très utile pour cela.

![image](https://user-images.githubusercontent.com/81978881/114319810-56549600-9b13-11eb-883e-14e1d74c96a7.png)

- Implémentation de l'application et des formulaires WindowsForms. 
   - Application démarre à partir d'une procédure Main qui lance le formulaire principal. Voir la configuration de l'Application dans la fenêtre des propriétés de la solution. Cela permet d'établir une correspondnance avec une application C# et ainsi de comparer les 2 languages, voir de faire une traduction VB --> C# assez facilement.
   - Les formulaires WindowsForms n'utilise pas le declarateur de variable `WithEvents` spécifique à VB mais ajoute explicitement les évenements du formulaire, de ces controles et le constructeur `New`. Cela n'empêche pas l'utilisation du concepteur de formulaire. D'une manière générale il n'est plus fait appel aux procédures, fonctions spécifiques à VB au travers l'arborescence de `Microsoft.VisualBasic`. Les espaces de noms correspondants ne sont pas importés. Voir la configuration des Références dans la fenêtre des propriétés de la solution. Tout est disponible dans le framework.
```vb.net
'Dans le désigner
   'Suppression par rapport au désigner VB de la déclaration d'une variable avec le déclarateur WithEvents
   'Friend WithEvents Button1 As Button
   'Ajout par rapport au designer VB de la déclaration normale d'une variable au lieu du declarateur WithEvents
   Friend Button1 As Button
    
'Dans le principal
   'procédure invisible en Winforms classique mais ajouter par le compilateur
   Friend Sub New()
      InitializeComponent()
      AjouterEvenements()
   End Sub
   
   'procédure à appeler dans le la Sub New() du formulaire. Invisible en Winforms classique mais ajouter par le compilateur
   Private Sub AjouterEvenements()
      AddHandler Me.Button1.Click, New EventHandler(AddressOf Button1_Click)
   End Sub
   
   'Suppression de la clause Handles
   Private Sub Button1_Click(sender As Object, e As EventArgs) 'Handles Button1.Click
   End Sub
```
Le control n'est pas disponible dans le concepteur de formulaire. Vous pouvez le remplacer par un control Panel afin d'obtenir les propriétés de mise en page que vous pourrez récupérer lors de la configuration du GLControl dans le code.
```vb
   'Ajout dans le New ou le Load du formulaire
   'création du control hors désigner
   RenduOpenGL = New GLControl(New GraphicsMode(), 3, 1, GraphicsContextFlags.Default) With 
   {
      .Dock = DockStyle.Fill,             'propriété de mise en page. Ici un seul control sur toute la surface client du formulaire
      .VSync = True                       'autre config concernant la qualité de l'affichage
   }
   'ajout du controle sur le formulaire
   Controls.Add(RenduOpenGL)
```
- Emulation Evenement UpdateFrame et Render Frame avec le GLControl
   - Timer. C'est une implémentation très facile à partir d'un timer Winforms. La vitesse obtenue à vide pour l'évenement Update est cependant assez basse mais largement suffisante pour toute application qui ne soit pas un jeu d'action.
   - évenement Idle. Cet évenement est émis par l'application juste avant avant qu'elle ne se repose autrement dit très souvent. Pour que cette implémentation fonctionne il faut que la boucle d'écoute du formulaire soit alimentée en permanence. Voir le code pour cela. La vitesse est tout à fait correcte. La fenêtre doit garder le focus en permance. 
   - Boucle de jeux. C'est la plus compliquée à implémenter et elle consomme beaucoup de ressources mais c'est celle qui offre les plus hautes performances. Si votre besoin de performance est à ce niveau, il vaudra mieux sans doute aller sur une GameWindows.
   - Les matrices de transformation. Un module annexe permet de transformer un point d'un modèle en pixel affiché sur l'écran. Cet exercice permet à la fois de suivre le cheminement du pipeline OpenGL et l'apprentissage des matrices de transformation. Une procédure concerne les matrices OpenTK. Une autre concerne les matrices OpenGL. Vous pouvez suivre le cheminement dans une session de débogage en pas à pas.

## Type de projet
- Projet VS 2017 Net Framework 4.8
- Dépendance Librairie OpenTK version 3.3.1. Les versions ultérieures sont pour Net.Core. Package NuGet à partir de la gestion des packages NuGet sous VS
- Dépendance Librairie OpenTK.GlControl version 3.1.0. Package NuGet à partir de la gestion des packages NuGet sous VS
- Dépendance Librairie OpenTK.Texte fournie avec le projet ou utilisation du code VB (traduction C# --> VB)
