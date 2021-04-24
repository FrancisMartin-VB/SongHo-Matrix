# Songho-Matrix
Ce projet est inspiré très fortement du site http://www.songho.ca/opengl et particulièrement du programme C++ : http://www.songho.ca/opengl/files/matrix.zip pour la partie dessin. 
Il permet d'avoir un aperçu de la mise en œuvre de la librairie OpentTK pour le Framework 4.8
Il se compose d'une partie commune qui concerne le dessin de la scène. Une GameWindow et 2 Forms avec un GLControl permettent d'afficher le dessin.
Le 1ère Forms émule les évenements UpdateFrame et RenderFrame absent du ControlGL à travers une minuterie ou l'évenement d'application Idle.
Le 2ème Forms implémente une boucle de jeux.
Les 3 rendus permettent d'avoir une animation du modèle manuelle ou automatique.

## Affichage de Texte
La dll OpenTK.Texte permet l'affichage de texte à partir de n'importe quelle police accessible sous windows. Le code VB correspondant est fourni. 
Le code C# support provient d'une archive de la version N°1.1.4 d'OpenTK.Compatibility `class TextPrinter` et ces dépendances que l'on peut trouver ici : https://sourceforge.net/projects/opentk/
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
   - Prise en compte des évenements UpdateFrame et RenderFrame sur la GameWindow --> G
- Touche fenêtre GLcontrol N°1
   - Démarage des évenements UpdateFrame et RenderFrame sur le Form1 avec l'émulation à partir de l'évenementd'application Idle --> I
  - Démarage des évenements UpdateFrame et RenderFrame sur le Form1 avec l'émulation à partir de d'une minuterie --> T
- Touche fenêtre GLcontrol N°2
   - Démarage des évenements UpdateFrame et RenderFrame sur le Form2 avec une boucle pour les 2 évenements --> B
   - Démarage des évenements UpdateFrame et RenderFrame sur le Form2 avec une boucle pour chaque évenement --> Ctrl+B

![image](https://user-images.githubusercontent.com/81978881/114317360-045a4300-9b08-11eb-8be9-669bc93e583d.png)

## Particularité du code
Pour ce 1er programme la partie dessin est simple et n'utilise que le mode OpenGL immédiat. Le principal est de voir l'implentation d'un rendu OpenGL et de comprendre les différents aspects de celle-ci. N'hésiter pas à lire les commentaires et à faire varier les différents paramètres dans le code afin de voir la variation sur les ressources de votre système. Le gestionnaire des tâches est très utile pour cela.

![image](https://user-images.githubusercontent.com/81978881/114319810-56549600-9b13-11eb-883e-14e1d74c96a7.png)

## Type de projet
- Projet VS 2017 Net Framework 4.8
- Dépendance Librairie OpenTK version 3.3.1. Les versions ultérieures sont pour Net.Core. Package NuGet à partir de la gestion des packages NuGet sous VS
- Dépendance Librairie OpenTK.GlControl version 3.1.0. Package NuGet à partir de la gestion des packages NuGet sous VS
- Dépendance Librairie OpenTK.Texte fournie avec le projet ou utilisation du code VB (traduction C# --> VB)
