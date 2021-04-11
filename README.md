Ce projet permet d'avoir un aperçu de la mise en œuvre de la librairie OpentTK en .net. Il est inspiré très fortement du site http://www.songho.ca/opengl et particulièrement du programme C++ : http://www.songho.ca/opengl/files/matrix.zip pour la partie dessin. 

Il se compose d'une partie commune qui concerne le dessin de la scène. Une GameWindow et 2 Forms avec un controlGL permettent d'afficher le dessin.
Le 1ère Forms émule les évenements UpdateFrame et RenderFrame absent du ControlGL à travers une minuterie ou au choix l'évenementd'application Idle.
Le 2ème Forms implémente une boucle de jeux.
Les 3 rendus permettent d'avoir une animation manuelle, ou automatique.

Il incopore une dll qui permet d'afficher du texte à partir de n'importe quelle police accessible sous windows ainsi que le code VB correspondant. Le code support provient d'une archive de la version N°1.1.4 d'OpenTK.Compatibility classe TextPrinter et ces dépendances que l'on peut trouver ici : https://sourceforge.net/projects/opentk/

F11 --> Passage en fenêtre maximisée et vice versa

F   --> rendu des triangles en Plein, Fil, Point

L   --> Epaisseur de 1 à 3

A   --> Arrêt des évenements UpdateFrame et RenderFrame

R   --> Animation du modèle (Rotation axe Y) soit en automatique soit en manuel

Augmentation des FPS d'animation --> +
   
Diminution des FPS d'animation --> -
   
G   --> Démarage des évenements UpdateFrame et RenderFrame sur la GameWindow


I   --> Démarage des évenements UpdateFrame et RenderFrame sur le Form1 avec l'émulation à partir de l'évenementd'application Idle

T   --> Démarage des évenements UpdateFrame et RenderFrame sur le Form1 avec l'émulation à partir de d'une minuterie


B   --> Démarage des évenements UpdateFrame et RenderFrame sur le Form2 avec une boucle pour les 2 évenements


![image](https://user-images.githubusercontent.com/81978881/114317360-045a4300-9b08-11eb-8be9-669bc93e583d.png)

Ctrl+B  Démarage des évenements UpdateFrame et RenderFrame sur le Form2 avec une boucle pour chaque évenement





