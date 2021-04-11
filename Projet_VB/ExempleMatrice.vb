Imports System.Runtime.InteropServices

''' <summary> Ce module sert uniquement à comprendre le pipeline OpenGL et les transformations
''' subit par les coordonnées d'un point d'un modèle afin d'être positionné sur l'écran </summary>
Module ExempleMatrice
    ''' <summary> Calcul la transformation d'un point exprimé en corrdonnées Modèle avec les functions OpenTK
    ''' Permet de comprendre le cheminement dans le pipeline OpenGL et les matrices OpenTK. 
    ''' Prépare à l'OpenGL moderne, aux shaders et aux Uniforme ''' </summary>
    Friend Sub Exemple_Matrices_OpenTK()
        Console.WriteLine(CrLf & "Exemple_Matrices_OpenTK()" & CrLf)
        Dim ModelMatrice, ViewMatrice, ModelViewMatrice, ProjectionMatrice, ModelViewProjectionMatrice As Matrix4
        'Position initiale de la caméra exprimée en coordonnées de l'espace monde
        ' on suppose que le point visé par la caméra est l'origine de l'espace monde
        Dim cameraAngleY = 105.0F 'en degrés et CCW (anti horaire)
        Dim cameraAngleX = 70.0F 'en degrés et  CCW (anti horaire)
        Dim cameraDistance = 6.0F ' en unité
        'Angle de vue vertical de la caméra en degré. 
        Dim FovY = 45.0F
        'Clip de profondeur de la zone de projection en unité
        Dim Near = 1.0F
        Dim Far = 100.0F
        'Dimensions en pixel du rendu OpenGL
        Dim W = 800, H = 600
        'Point initial appartenant à un objet à dessiner exprimé en coordonnées de l'espace modèle
        'c'est en fait un vector3 (x, y, z) auquel on rajoute W=1 pour faire les calculs avec les matrix4
        Dim VInit = New Vector4(0, 0, 2, 1)
        Console.WriteLine("VInit : " & VInit.ToString & CrLf)
        'Point qui contient le résultat des différentes transformation du point initial
        Dim VTR As Vector4

        'Avec OpenTK on construit la matrice de transformation en multipliant les transformations dans l'ordre désiré. C'est l'inverse d'OPENGL
        'Dans ce cas précis l'ordre n'a pas d'importance car les 2 transformations envisagées concerne le même axe. 
        'Matrice Modèle : 1ére transformation : Rotation Y de 45°
        ModelMatrice = Matrix4.CreateRotationY(45.0F * DegToRad)
        'A noter que le vecteur initial est à gauche de la multiplication avec la matrice. C'est l'inverse d'OPENGL
        VTR = VInit * ModelMatrice 'voir le résultat de la transformation sur le vecteur VTR. 

        'Matrice Modèle : 2ème transformation : Translation Y d'une unité. 
        ModelMatrice *= Matrix4.CreateTranslation(0.0F, 1.0F, 0.0F)
        VTR = VInit * ModelMatrice 'voir le résultat de la transformation sur le vecteur VTR.  
        Console.WriteLine("ModelMatrice : " & CrLf & ModelMatrice.ToString & CrLf)
        VoirMemoire(VTR, ModelMatrice)

        'Matrice Vue : 1ère transformation : Rotation Y de 105°. 'Voir la scéne sur le coté et arrière
        ViewMatrice = Matrix4.CreateRotationY(cameraAngleY * DegToRad)
        VTR = VInit * ViewMatrice 'voir le résultat de la transformation sur le vecteur VTR.  
        'Matrice Vue : 2ème transformation : Rotation X de 70°. 'Voir la scéne par au dessus
        ViewMatrice *= Matrix4.CreateRotationX(cameraAngleX * DegToRad)
        VTR = VInit * ViewMatrice 'voir le résultat de la transformation sur le vecteur VTR. 
        'Matrice Vue : 3ème transformation : Translation Z de 6 unités. 'Voir la scène en entier
        ViewMatrice *= Matrix4.CreateTranslation(0, 0, -cameraDistance)
        Console.WriteLine("ViewMatrice : " & CrLf & ViewMatrice.ToString & CrLf)
        VTR = VInit * ViewMatrice 'voir le résultat de la transformation sur le vecteur VTR.  


        'Matrice Modèle-Vue
        ModelViewMatrice = ModelMatrice * ViewMatrice
        Console.WriteLine("ModelViewMatrice : " & CrLf & ModelViewMatrice.ToString & CrLf)
        VTR = VInit * ModelViewMatrice 'voir le résultat de la transformation sur le vecteur VTR. 

        'Matrice Projection : En perspective
        ProjectionMatrice = Matrix4.CreatePerspectiveFieldOfView(FovY * DegToRad, CSng(W / H), 1.0F, 100.0F)
        Console.WriteLine("ProjectionMatrice : " & CrLf & ProjectionMatrice.ToString & CrLf)


        'Matrice Modèle-Vue-Projection
        ModelViewProjectionMatrice = ModelViewMatrice * ProjectionMatrice
        VTR = VInit * ModelViewProjectionMatrice 'voir le résultat de la transformation sur le vecteur VTR.  
        Console.WriteLine("ModelViewProjectionMatrice : " & CrLf & ModelViewProjectionMatrice.ToString & CrLf)
        Console.WriteLine("VInit * ModelViewProjectionMatrice : " & VTR.ToString & CrLf)

        'transformation en pixel de la fenêtre d'affichage
        Coordonnees_ModelViewProjectionToViewPort(VTR, W, H)
    End Sub

    ''' <summary> Calcul la transformation d'un point exprimé en corrdonnées Modèle avec les functions opengl
    ''' Ne peut être appelée qu'à partir d'un rendu OpenGL initialisé. </summary>
    Sub Exemple_Matrices_OpenGL()
        Console.WriteLine(CrLf & "Exemple_Matrices_OpenGL()" & CrLf)
        'Position initiale de la caméra exprimée en coordonnées de l'espace monde
        Dim cameraAngleY = 105.0F
        Dim cameraAngleX = 70.0F
        Dim cameraDistance = 6.0F
        'Angle de vue vertical de la caméra en degré. 
        Dim FovY = 45.0F
        'Clip de la zone de projection en unité
        Dim Near = 1.0F
        Dim Far = 100.0F
        'Dimensions en pixel du rendu OpenGL
        Dim W = 800, H = 600
        'Point initial appartenant à un objet à dessiner exprimé en coordonnées de l'espace modèle
        Dim VInit = New Vector4(0, 0, 2, 1)

        'on va travailler avec la matrice des textures pour cet example. OpenGL travaille aussi avec ModelView, Projection ou Color
        'La matrice texture va servir de support pour les calculs mais en réel il faut sélectionner la bonne matrice avant de faire les calculs
        GL.MatrixMode(MatrixMode.Texture)

        'Matrice Modèle : initialisée à identity
        GL.LoadIdentity()
        'Matrice Modèle : 1ère transformation : Translation Y d'une unité. 
        GL.Translate(0.0F, 1.0F, 0.0F)
        'Matrice Modèle : 2ème transformation : Rotation Y de 45°
        GL.Rotate(45.0F, 0.0F, 1.0F, 0.0F)
        'sauvegarde de la matrice pour plutard
        Dim MatriceModel As Matrix4
        GL.GetFloat(GetPName.TextureMatrix, MatriceModel)
        Console.WriteLine("ModelMatrice : " & CrLf & MatriceModel.ToString & CrLf)


        'Matrice Vue : initialisée à identity
        GL.LoadIdentity()
        'Matrice Vue : 1ère transformation : Translation Z de 6 unités. 'Voir la scène en entier
        GL.Translate(0.0F, 0.0F, -cameraDistance) 'TZ
        'Matrice Vue : 2ème transformation : Rotation X de 70°. 'Voir la scéne par au dessus
        GL.Rotate(cameraAngleX, 1.0F, 0.0F, 0.0F) 'RX
        'Matrice Vue : 3ème transformation : Rotation Y de 105°. 'Voir la scéne sur le coté et arrière
        GL.Rotate(cameraAngleY, 0.0F, 1.0F, 0.0F) 'RY
        'sauvegarde de la matrice pour plutard. 
        Dim MatriceView As Matrix4
        GL.GetFloat(GetPName.TextureMatrix, MatriceView)
        Console.WriteLine("ViewMatrice : " & CrLf & MatriceView.ToString & CrLf)


        'Matrice Modèle-Vue
        GL.MultMatrix(MatriceModel)
        'sauvegarde de la matrice pour plutard. 
        'En mode immédiat d'opengL il n'y a pas besoin de sauvegarder, le pipeline a un accès à cette matrice en tant que variable (Uniforme)
        Dim MatriceViewModel As Matrix4
        GL.GetFloat(GetPName.TextureMatrix, MatriceViewModel)
        Console.WriteLine("ModelViewMatrice : " & CrLf & MatriceViewModel.ToString & CrLf)


        'Matrice Projection : En perspective
        'Matrice Projection : initialisée à identity
        GL.LoadIdentity()
        'Matrice Projection : calcul des paramètres largeur et hauteur
        Dim tangent As Double = Math.Tan(FovY / 2 * DegToRad) ' tangent of half fovY
        Dim height As Double = Near * tangent ' half height of near plane
        Dim width As Double = height * (W / H) ' half width of near plane
        GL.Frustum(-width, width, -height, height, Near, Far)
        'sauvegarde de la matrice pour plutard. 
        'En mode immédiat d'opengL il n'y a pas besoin de sauvegarder, le pipeline a un accès à cette matrice en tant que variable (Uniforme)
        Dim MatriceProjection As Matrix4
        GL.GetFloat(GetPName.TextureMatrix, MatriceProjection)
        Console.WriteLine("ProjectionMatrice : " & CrLf & MatriceProjection.ToString & CrLf)


        'Matrice Modèle-Vue-Projection
        'En mode immédiat d'opengL il n'y a pas besoin de sauvegarder, le pipeline la calcule en interne
        GL.MultMatrix(MatriceViewModel)
        'sauvegarde de la matrice pour plutard. 
        'En mode immédiat d'opengL il n'y a pas besoin de sauvegarder, le pipeline la calcul en interne
        Dim MatriceProjectionViewModel As Matrix4
        GL.GetFloat(GetPName.TextureMatrix, MatriceProjectionViewModel)
        Console.WriteLine("ModelViewProjectionMatrice : " & CrLf & MatriceProjectionViewModel.ToString & CrLf)

        'récupération de la valeur du vecteur transformé sous OpenGL
        'une matrice peut être vu aussi comme un ensemble de 4 vector4. 
        'On place celui qui nous interesse sur la 1ère ligne, les 3 autres étant Vector4.zero
        Dim MatriceEchange As Matrix4 = New Matrix4() With {.Row0 = VInit}
        GL.MultMatrix(MatriceEchange)
        GL.GetFloat(GetPName.TextureMatrix, MatriceEchange)

        Dim VTR As Vector4 = MatriceEchange.Row0
        Console.WriteLine("VInit * ModelViewProjectionMatrice : " & VTR.ToString & CrLf)

        Coordonnees_ModelViewProjectionToViewPort(VTR, W, H)

        'on ré-initialise la matrice des textures
        GL.LoadIdentity()
    End Sub

    ''' <summary> transforme un vecteur4 en coordonnées ModelViewProjection en Pixel X,Y sur la fenêtre d'affichage. 0,0 en haut à gauche 
    ''' Cette partie est interne au pipeline OpenGL </summary>
    ''' <param name="VTR"> Vecteur à transformer </param>
    ''' <param name="W"> Largeur en pixel de la fenêtre d'affichage </param>
    ''' <param name="H"> Hauteur en pixel de la fenêtre d'affichage </param>
    Private Sub Coordonnees_ModelViewProjectionToViewPort(VTR As Vector4, W As Integer, H As Integer)
        'Transformation en coordonnées normalisées de fenêtre. 
        'Pour X et Y elles sont comprisent entre -1 et +1. Z sert au test de profondeur 
        Dim NDC As Vector3 = New Vector3(VTR.X / VTR.W, VTR.Y / VTR.W, -VTR.Z)

        'transformation en coordonnées de la fenêtre de rendu OpenGL exprimée en pixel
        Dim XPixel As Integer = CInt(Math.Ceiling((NDC.X + 1) * W / 2))
        Dim YPixel As Integer = CInt(Math.Ceiling((1 - NDC.Y) * H / 2))
        Console.WriteLine("Coordonnées projetées : " & VTR.ToString)
        Console.WriteLine("Coordonnées viewPort --> X : " & XPixel & ", Y : " & YPixel)
    End Sub

    ''' <summary> transforme un vector4 et une matrix4 en tableau d'octets 
    ''' cela permet de voir l'organisation de ces structures en mémoire </summary>
    ''' <param name="Vecteur"></param>
    ''' <param name="Matrice"></param>
    Private Sub VoirMemoire(Vecteur As Vector4, Matrice As Matrix4)
        'Afin de voir comment sont organisés en mémoire les vecteurs et les matrices d'OpenTK
        Dim DataBytes As Byte()
        Dim NbBytes As Integer
        NbBytes = Vector4.SizeInBytes
        ReDim DataBytes(NbBytes - 1)
        BlockCopy(Vecteur, 0, DataBytes, 0, NbBytes)
        Dim V4 = BytesToVecteur4(DataBytes)
        'une matrix4 = 4 vector4
        NbBytes = Vector4.SizeInBytes * 4
        ReDim DataBytes(NbBytes - 1)
        BlockCopy(Matrice, 0, DataBytes, 0, NbBytes)
        Dim M_Retour = BytesToMatrix4(DataBytes)
    End Sub

    ''' <summary> Correspond à l'opération MatriceGauche * MatriceDroite implenté dans OpenTK </summary>
    ''' <param name="l"> matrice à gauche de l'opérateur * </param>
    ''' <param name="r"> matrice à droite de l'opérateur * </param>
    Private Function MultiplierMatrices(l As Matrix4, r As Matrix4) As Matrix4
        Dim Result As Matrix4
        Result.Row0.X = (l.Row0.X * r.Row0.X) + (l.Row0.Y * r.Row1.X) + (l.Row0.Z * r.Row2.X) + (l.Row0.W * r.Row3.X)
        Result.Row0.Y = (l.Row0.X * r.Row0.Y) + (l.Row0.Y * r.Row1.Y) + (l.Row0.Z * r.Row2.Y) + (l.Row0.W * r.Row3.Y)
        Result.Row0.Z = (l.Row0.X * r.Row0.Z) + (l.Row0.Y * r.Row1.Z) + (l.Row0.Z * r.Row2.Z) + (l.Row0.W * r.Row3.Y)
        Result.Row0.W = (l.Row0.X * r.Row0.W) + (l.Row0.Y * r.Row1.W) + (l.Row0.Z * r.Row2.W) + (l.Row0.W * r.Row3.W)
        Result.Row1.X = (l.Row1.X * r.Row0.X) + (l.Row1.Y * r.Row1.X) + (l.Row1.Z * r.Row2.X) + (l.Row1.W * r.Row3.X)
        Result.Row1.Y = (l.Row1.X * r.Row0.Y) + (l.Row1.Y * r.Row1.Y) + (l.Row1.Z * r.Row2.Y) + (l.Row1.W * r.Row3.Y)
        Result.Row1.Z = (l.Row1.X * r.Row0.Z) + (l.Row1.Y * r.Row1.Z) + (l.Row1.Z * r.Row2.Z) + (l.Row1.W * r.Row3.Z)
        Result.Row1.W = (l.Row1.X * r.Row0.W) + (l.Row1.Y * r.Row1.W) + (l.Row1.Z * r.Row2.W) + (l.Row1.W * r.Row3.W)
        Result.Row2.X = (l.Row2.X * r.Row0.X) + (l.Row2.Y * r.Row1.X) + (l.Row2.Z * r.Row2.X) + (l.Row2.W * r.Row3.X)
        Result.Row2.Y = (l.Row2.X * r.Row0.Y) + (l.Row2.Y * r.Row1.Y) + (l.Row2.Z * r.Row2.Y) + (l.Row2.W * r.Row3.Y)
        Result.Row2.Z = (l.Row2.X * r.Row0.Z) + (l.Row2.Y * r.Row1.Z) + (l.Row2.Z * r.Row2.Z) + (l.Row2.W * r.Row3.Z)
        Result.Row2.W = (l.Row2.X * r.Row0.W) + (l.Row2.Y * r.Row1.W) + (l.Row2.Z * r.Row2.W) + (l.Row2.W * r.Row3.W)
        Result.Row3.X = (l.Row3.X * r.Row0.X) + (l.Row3.Y * r.Row1.X) + (l.Row3.Z * r.Row2.X) + (l.Row3.W * r.Row3.X)
        Result.Row3.Y = (l.Row3.X * r.Row0.Y) + (l.Row3.Y * r.Row1.Y) + (l.Row3.Z * r.Row2.Y) + (l.Row3.W * r.Row3.Y)
        Result.Row3.Z = (l.Row3.X * r.Row0.Z) + (l.Row3.Y * r.Row1.Z) + (l.Row3.Z * r.Row2.Z) + (l.Row3.W * r.Row3.Z)
        Result.Row3.W = (l.Row3.X * r.Row0.W) + (l.Row3.Y * r.Row1.W) + (l.Row3.Z * r.Row2.W) + (l.Row3.W * r.Row3.W)
        Return Result
    End Function

    ''' <summary> Correspond à l'opération Vecteur4 * Matrice4 implenté dans OpenTK </summary>
    ''' <param name="vec"> veteur à gauche de l'opérateur * </param>
    ''' <param name="mat"> matrice à droite de l'opérateur * </param>
    Private Function MultiplierVecteurMatrice(ByVal vec As Vector4, ByVal mat As Matrix4) As Vector4
        Return New Vector4((vec.X * mat.Row0.X) + (vec.Y * mat.Row1.X) + (vec.Z * mat.Row2.X) + (vec.W * mat.Row3.X),
                           (vec.X * mat.Row0.Y) + (vec.Y * mat.Row1.Y) + (vec.Z * mat.Row2.Y) + (vec.W * mat.Row3.Y),
                           (vec.X * mat.Row0.Z) + (vec.Y * mat.Row1.Z) + (vec.Z * mat.Row2.Z) + (vec.W * mat.Row3.Z),
                           (vec.X * mat.Row0.W) + (vec.Y * mat.Row1.W) + (vec.Z * mat.Row2.W) + (vec.W * mat.Row3.W))
    End Function

    ''' <summary> Correspond à l'opération  Matrice4 * Vecteur4 implenté dans OpenTK </summary>
    ''' <param name="mat"> matrice à gauche de l'opérateur * </param>
    ''' <param name="vec"> veteur à droite de l'opérateur * </param>
    Private Function MultiplierMatriceVecteur(mat As Matrix4, vec As Vector4) As Vector4
        Return New Vector4((mat.Row0.X * vec.X) + (mat.Row0.Y * vec.Y) + (mat.Row0.Z * vec.Z) + (mat.Row0.W * vec.W),
                           (mat.Row1.X * vec.X) + (mat.Row1.Y * vec.Y) + (mat.Row1.Z * vec.Z) + (mat.Row1.W * vec.W),
                           (mat.Row2.X * vec.X) + (mat.Row2.Y * vec.Y) + (mat.Row2.Z * vec.Z) + (mat.Row2.W * vec.W),
                           (mat.Row3.X * vec.X) + (mat.Row3.Y * vec.Y) + (mat.Row3.Z * vec.Z) + (mat.Row3.W * vec.W))
    End Function

    ''' <summary> Transforme un tableau de 4*4 octets en Vector4 </summary>
    ''' <param name="Data"> le tableau de 16 octets à transformer</param>
    Private Function BytesToVecteur4(Data() As Byte) As Vector4
        Dim SrcHandle As GCHandle = GCHandle.Alloc(Data, GCHandleType.Pinned)
        Dim V4 As Vector4 = Marshal.PtrToStructure(Of Vector4)(SrcHandle.AddrOfPinnedObject)
        SrcHandle.Free()
        Return V4
    End Function

    ''' <summary> Transforme un tableau de 4*4*4 octets en Matrix4 </summary>
    ''' <param name="Data"> le tableau de 64 octets à transformer </param>
    Private Function BytesToMatrix4(Data() As Byte) As Matrix4
        Dim SrcHandle As GCHandle = GCHandle.Alloc(Data, GCHandleType.Pinned)
        Dim M4 As Matrix4 = Marshal.PtrToStructure(Of Matrix4)(SrcHandle.AddrOfPinnedObject)
        SrcHandle.Free()
        Return M4
    End Function

    ''' <summary> Permet de copier une variable managée sous la forme d'un tableau d'octet afin de regearder l'organisation en mémoire </summary>
    ''' <param name="Src"> Matrix4 ou Vector4 ou tout autre variable ou structure managée </param>
    ''' <param name="IndexSRC"> Index de départ dans la source. Généralement 0 </param>
    ''' <param name="Dest"> Obligatoire un Tableau de bytes destiné à recevoir le résultat </param>
    ''' <param name="IndexDest"> Index de départ dans le tableau de destination. Généralement 0 </param>
    ''' <param name="NbBytes"> Nb d'octets à copier. Généralement le taille en octets de la source </param>
    Friend Sub BlockCopy(Src As Object, IndexSRC As Integer, Dest As Object, IndexDest As Integer, NbBytes As Integer)
        Dim SrcHandle As GCHandle = GCHandle.Alloc(Src, GCHandleType.Pinned)
        Dim DestHandle As GCHandle = GCHandle.Alloc(Dest, GCHandleType.Pinned)
        CopyMemory(DestHandle.AddrOfPinnedObject + IndexDest, SrcHandle.AddrOfPinnedObject + IndexSRC, NbBytes)
        SrcHandle.Free()
        DestHandle.Free()
    End Sub

    ''' <summary> fonction du systeme d'exploitation pour copier un block mémoire d'un endroit à un autre </summary>
    <DllImport("kernel32.dll", SetLastError:=True, EntryPoint:="CopyMemory")>
    Friend Sub CopyMemory(destination As IntPtr, source As IntPtr, length As Integer)
    End Sub
End Module