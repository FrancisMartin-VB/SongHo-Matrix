'Pour afficher du texte sur le viewport
Imports SongHoMatrix_OpenTK.Texte 'prise en compte du code à la place de la dll
'Imports OpenTK.Texte 'ou prise en compte de la dll à la place du code
Module SongHo_Matrix
#Region "Privée"
    'Variables
    Private Const fmt As String = " 0.000;-0.000"
    Private MatriceModel, MatriceView, MatriceModelView, MatriceProjection, MatriceProjectionText As Matrix4
    Private W, H As Integer
    Private PoliceMatrice, PoliceAnimation As Font, Texte As TexteGL
    Private GLExtensions As ExtensionsGL
    'procédures
    ''' <summary> dessine le model sur la scène avec la couleur spécifiée </summary>
    Private Sub DessinerModel(r As Single, g As Single, b As Single, a As Single)
        GL.Color4(r, g, b, a)
        GL.LineWidth(TailleLinePointPolygone)
        GL.PointSize(TailleLinePointPolygone)
        GL.Begin(PrimitiveType.Triangles)
        GL.Normal3(0.6667F, 0.6667F, 0.3334F)
        GL.Vertex3(1, 0, 0)
        GL.Vertex3(0, 1, 0)
        GL.Vertex3(0, 0, 2)

        GL.Normal3(-0.6667F, 0.6667F, 0.3334F)
        GL.Vertex3(-1, 0, 0)
        GL.Vertex3(0, 0, 2)
        GL.Vertex3(0, 1, 0)

        GL.Normal3(0, 0, -1)
        GL.Vertex3(1, 0, 0)
        GL.Vertex3(0, 0, 2)
        GL.Vertex3(-1, 0, 0)

        GL.Normal3(0, -1, 0)
        GL.Vertex3(1, 0, 0)
        GL.Vertex3(-1, 0, 0)
        GL.Vertex3(0, 1, 0)
        GL.End()
        GL.LineWidth(1.0F)
    End Sub
    ''' <summary> dessine la grille du plan X-Z sur la scène de la taille et du pas spécifié </summary>
    Private Sub DessinerGrille(Optional size As Single = 10, Optional Pas As Single = 1)
        ' disable lighting
        GL.Disable(EnableCap.Lighting)

        GL.LineWidth(1.0F)
        ' 20x20 grid
        GL.Begin(PrimitiveType.Lines)
        GL.Color3(0.5F, 0.5F, 0.5F)
        'grille plan XZ
        For i As Single = Pas To size Step Pas
            GL.Vertex3(-size, 0, i) ' lines parallel to X-axis
            GL.Vertex3(size, 0, i)
            GL.Vertex3(-size, 0, -i) ' lines parallel to X-axis
            GL.Vertex3(size, 0, -i)

            GL.Vertex3(i, 0, -size) ' lines parallel to Z-axis
            GL.Vertex3(i, 0, size)
            GL.Vertex3(-i, 0, -size) ' lines parallel to Z-axis
            GL.Vertex3(-i, 0, size)
        Next

        ' x-axis
        GL.Color3(Color.Red)
        GL.Vertex3(-size, 0, 0)
        GL.Vertex3(size, 0, 0)
        ' y-axis
        GL.Color3(Color.Green)
        GL.Vertex3(0.0F, 0.5F, 0.0F)
        GL.Vertex3(0.0F, -0.5F, 0.0F)
        ' z-axis
        GL.Color3(Color.Blue)
        GL.Vertex3(0, 0, -size)
        GL.Vertex3(0, 0, size)

        GL.End()
        GL.LineWidth(1.0F)
        ' enable lighting back
        GL.Enable(EnableCap.Lighting)
    End Sub
    ''' <summary> dessine les axes du modele sur la scène de la taille spécifié </summary>
    Private Sub DessinerAxes(Optional size As Single = 2.5)
        GL.DepthFunc(DepthFunction.Always) ' to avoid visual artifacts with grid lines
        GL.Disable(EnableCap.Lighting)

        ' draw axis
        GL.LineWidth(3.0F)
        GL.Begin(PrimitiveType.Lines)
        GL.Color3(Color.Salmon) 'axe des X en rouge
        GL.Vertex3(0, 0, 0)
        GL.Vertex3(size, 0, 0)
        GL.Color3(Color.GreenYellow) ' axe des Y en Vert
        GL.Vertex3(0, 0, 0)
        GL.Vertex3(0, size, 0)
        GL.Color3(Color.BlueViolet) ' axe des Z en bleu
        GL.Vertex3(0, 0, 0)
        GL.Vertex3(0, 0, size)
        GL.End()
        GL.LineWidth(1.0F)
        ' draw arrows(actually big square dots)
        GL.PointSize(5.0F)
        GL.Begin(PrimitiveType.Points)
        GL.Color3(Color.Red)
        GL.Vertex3(size, 0, 0)
        GL.Color3(Color.Green)
        GL.Vertex3(0, size, 0)
        GL.Color3(Color.Blue)
        GL.Vertex3(0, 0, size)
        GL.End()
        GL.PointSize(1.0F)
        ' restore default settings
        GL.Enable(EnableCap.Lighting)
        GL.DepthFunc(DepthFunction.Lequal)
    End Sub
    ''' <summary> Ecrit les matrice View et model ainsi que le Update Frame et compteur de FPS </summary>
    Private Sub EcrireMatrices()
        ' sauvegarde de la matrice ModelVievw actuelle
        GL.PushMatrix()
        ' on l'initialise à rien
        GL.LoadIdentity()

        ' sauvegarde de la matrice Projection actuelle 
        GL.MatrixMode(MatrixMode.Projection)
        GL.PushMatrix()
        ' on charge la matrice de projection de l'affichage du text
        GL.LoadMatrix(MatriceProjectionText)

        'pour éviter des artefacts quand les polygones sont dessinnés en lines ou points
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill)
        ' pour ne pas avoir d'altération de la couleur du texte
        GL.Disable(EnableCap.Lighting)
        'Ecriture de la matrice Vue
        Dim deltaY As Single = 0
        Texte.Ecrire("     === View Matrix ===", PoliceMatrice, Color.FromArgb(255, Color.White), New RectangleF(0F,
                   PoliceMatrice.Height * 0, 400.0F, 0F), TextPrinterOptions.Default)
        Texte.Ecrire($"[{MatriceView.M11.ToString(fmt)} {MatriceView.M21.ToString(fmt)} {MatriceView.M31.ToString(fmt)} {MatriceView.M41.ToString(fmt)} ]",
                   PoliceMatrice, Color.FromArgb(255, Color.White), New RectangleF(0F, deltaY + PoliceMatrice.Height * 1, 400.0F, 0F), TextPrinterOptions.Default)
        Texte.Ecrire($"[{MatriceView.M12.ToString(fmt)} {MatriceView.M22.ToString(fmt)} {MatriceView.M32.ToString(fmt)} {MatriceView.M42.ToString(fmt)} ]",
                   PoliceMatrice, Color.FromArgb(255, Color.White), New RectangleF(0F, deltaY + PoliceMatrice.Height * 2, 400.0F, 0F), TextPrinterOptions.Default)
        Texte.Ecrire($"[{MatriceView.M13.ToString(fmt)} {MatriceView.M23.ToString(fmt)} {MatriceView.M33.ToString(fmt)} {MatriceView.M43.ToString(fmt)} ]",
                   PoliceMatrice, Color.FromArgb(255, Color.White), New RectangleF(0F, deltaY + PoliceMatrice.Height * 3, 400.0F, 0F), TextPrinterOptions.Default)
        Texte.Ecrire($"[{MatriceView.M14.ToString(fmt)} {MatriceView.M24.ToString(fmt)} {MatriceView.M34.ToString(fmt)} {MatriceView.M44.ToString(fmt)} ]",
                   PoliceMatrice, Color.FromArgb(255, Color.White), New RectangleF(0F, deltaY + PoliceMatrice.Height * 4, 400.0F, 0F), TextPrinterOptions.Default)

        'Ecriture de la matrice Model
        deltaY = H - PoliceMatrice.Height * 5.1F
        Texte.Ecrire("     === View Model ===", PoliceMatrice, Color.FromArgb(255, Color.White), New RectangleF(0F, deltaY +
                   PoliceMatrice.Height * 0, 400.0F, 0F), TextPrinterOptions.NoCache)
        Texte.Ecrire($"[{MatriceModel.M11.ToString(fmt)} {MatriceModel.M21.ToString(fmt)} {MatriceModel.M31.ToString(fmt)} {MatriceModel.M41.ToString(fmt)} ]",
                   PoliceMatrice, Color.FromArgb(255, Color.White), New RectangleF(0F, deltaY + PoliceMatrice.Height * 1, 400.0F, 0F), TextPrinterOptions.Default)
        Texte.Ecrire($"[{MatriceModel.M12.ToString(fmt)} {MatriceModel.M22.ToString(fmt)} {MatriceModel.M32.ToString(fmt)} {MatriceModel.M42.ToString(fmt)} ]",
                   PoliceMatrice, Color.FromArgb(255, Color.White), New RectangleF(0F, deltaY + PoliceMatrice.Height * 2, 400.0F, 0F), TextPrinterOptions.Default)
        Texte.Ecrire($"[{MatriceModel.M13.ToString(fmt)} {MatriceModel.M23.ToString(fmt)} {MatriceModel.M33.ToString(fmt)} {MatriceModel.M43.ToString(fmt)} ]",
                   PoliceMatrice, Color.FromArgb(255, Color.White), New RectangleF(0F, deltaY + PoliceMatrice.Height * 3, 400.0F, 0F), TextPrinterOptions.Default)
        Texte.Ecrire($"[{MatriceModel.M14.ToString(fmt)} {MatriceModel.M24.ToString(fmt)} {MatriceModel.M34.ToString(fmt)} {MatriceModel.M44.ToString(fmt)} ]",
                   PoliceMatrice, Color.FromArgb(255, Color.White), New RectangleF(0F, deltaY + PoliceMatrice.Height * 4, 400.0F, 0F), TextPrinterOptions.Default)

        'Ecriture du type de updateFrame
        Texte.Ecrire(EventFrame, PoliceAnimation, Color.FromArgb(255, Color.Green), New RectangleF(W - 200.0F, 0F, 200.0F, 0F), TextPrinterOptions.Default, TextAlignment.Far)

        'écriture du FPS du renderframe
        Texte.Ecrire(TextFPS, PoliceAnimation, Color.FromArgb(255, Color.Green), New RectangleF(W - 400.0F, H - PoliceAnimation.Height, 400.0F, 0F), TextPrinterOptions.Default, TextAlignment.Far)

        GL.Enable(EnableCap.Lighting)
        GL.PolygonMode(MaterialFace.FrontAndBack, Rendu)
        ' restore projection matrix
        GL.PopMatrix() ' restore to previous projection matrix

        ' restore modelview matrix
        GL.MatrixMode(MatrixMode.Modelview) ' switch to modelview matrix
        GL.PopMatrix() ' restore to previous modelview matrix
    End Sub
    ''' <summary> autorise les différents états d'OpenGL dont on a besoin </summary>
    Private Sub InitialiserEnablesGL()
        GL.Enable(EnableCap.DepthTest)
        GL.Enable(EnableCap.AlphaTest)
        GL.Enable(EnableCap.CullFace)
        GL.Enable(EnableCap.Lighting)
        GL.Enable(EnableCap.Texture2D)
        GL.Enable(EnableCap.CullFace)
        GL.Enable(EnableCap.Blend)
        GL.Enable(EnableCap.LineSmooth)
        GL.Enable(EnableCap.ColorMaterial)
    End Sub
    ''' <summary> initialise les différents états d'OpenGL dont on a besoin </summary>
    Private Sub InitialiserEtatsGL()
        GL.ShadeModel(ShadingModel.Smooth)
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4)
        GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse)
        GL.PolygonMode(MaterialFace.FrontAndBack, Rendu)
        GL.ClearColor(0.0F, 0.0F, 0.0F, 0.0F)
        GL.BlendColor(1.0, 1.0, 1.0, 1.0)
        GL.ClearStencil(0)
        GL.ClearDepth(1)
        GL.DepthFunc(DepthFunction.Lequal)
        GL.AlphaFunc(AlphaFunction.Gequal, 0.09999999F)
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)
    End Sub
    ''' <summary> initialise une source lumineuse dont on a besoin </summary>
    Private Sub InitialiserLightsGL()
        ' set up light colors (ambient, diffuse, specular)
        Dim lightKa As Color4 = New Color4(0.2F, 0.2F, 0.2F, 1.0F) ' ambient light
        Dim lightKd As Color4 = New Color4(0.7F, 0.7F, 0.7F, 1.0F) ' diffuse light
        Dim lightKs As Color4 = New Color4(1, 1, 1, 1) ' specular light
        GL.Light(LightName.Light0, LightParameter.Ambient, lightKa)
        GL.Light(LightName.Light0, LightParameter.Diffuse, lightKd)
        GL.Light(LightName.Light0, LightParameter.Specular, lightKs)

        ' position the light
        Dim lightPos As Vector4 = New Vector4(0, 0, 20, 1) ' positional light
        GL.Light(LightName.Light0, LightParameter.Position, lightPos)
        GL.Enable(EnableCap.Light0) ' MUST enable each light source after configuration
    End Sub
    ''' <summary> initialise le comportement d'OpenGL </summary>
    Private Sub InitialiserHintGL()
        'glHint(GL_PERSPECTIVE_CORRECTION_HINT, GL_NICEST);
        GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest)
        'glHint(GL_LINE_SMOOTH_HINT, GL_NICEST);
        GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest)
        'glHint(GL_POLYGON_SMOOTH_HINT, GL_NICEST);
        GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest)
    End Sub
#End Region
    'variables partagées avec le renduOpenGL. Elles permettent soit les modifications de la scène soit la manière de la voir
    Friend Const WidthInit As Integer = 800
    Friend Const HeightInit As Integer = 600
    'gère le rendu des polygones : Triangle, soit plein, soit les arrêtes soit des points et la largeur de dessin
    Friend Rendu As PolygonMode
    Friend TailleLinePointPolygone As Single
    'gère le déplacement de la caméra
    Friend LastMouse As PointF
    Friend cameraAngleX, cameraAngleY, cameraDistance, PasX, PasY, PasDist, FovY As Single
    'gère le déplacement du modèle
    Friend RotationModelY As Single

    'gère le texte à afficher pour l'animation et les FPS
    Friend EventFrame, TextFPS As String
    Friend VersionOpenGL As String
    'procédures
    ''' <summary> initialise une source lumineuse dont on a besoin </summary>
    Friend Sub InitialiserRenduGL()
        'Partie concernant l'initalisation des variables partagées avec le Rendu d'OpenGL
        RotationModelY = 45.0F
        TailleLinePointPolygone = 1.0F
        PasX = 1.0F
        PasY = 1.0F
        PasDist = 0.2F
        cameraAngleY = 105.0F
        cameraAngleX = 70.0F
        cameraDistance = 6.0F
        FovY = 45.0F
        PoliceMatrice = New Font("Consolas", 10, FontStyle.Regular)
        PoliceAnimation = New Font("Comic Sans MS", 18, FontStyle.Regular)
        Texte = New TexteGL(TextQuality.High)
        Rendu = PolygonMode.Fill
        'Partie concernant la configuration d'OpenGL
        InitialiserEnablesGL()
        InitialiserHintGL()
        InitialiserEtatsGL()
        InitialiserLightsGL()
        GLExtensions = New ExtensionsGL()
        VersionOpenGL = GL.GetString(StringName.Version) & ", Rendu : " & GL.GetString(StringName.Renderer)
    End Sub
    ''' <summary> détermine la surface d'affichage en fonction des dimensions de la fenêtre et met la martice de projection en relation</summary>
    Friend Sub SetViewPort(Taille As Size)
        W = Taille.Width
        H = Taille.Height
        'on dessine sur l'ensemble de la surface de la fenêtre
        GL.Viewport(0, 0, W, H)
        'determination de la matrice de projection pour l'écriture de texte sur le viewport
        MatriceProjectionText = Matrix4.CreateOrthographicOffCenter(0F, W, H, 0, -1.0F, 1.0F)
        'détermination et affectation de la matrice de projection pour le dessin sur le viewport
        MatriceProjection = Matrix4.CreatePerspectiveFieldOfView(FovY * DegToRad, CSng(W / H), 1.0F, 100.0F)
        GL.MatrixMode(MatrixMode.Projection)
        GL.LoadMatrix(MatriceProjection)
    End Sub
    ''' <summary> dessine la scène </summary>
    Friend Sub DessinerScene()
        '// clear buffer
        GL.Clear(ClearBufferMask.ColorBufferBit Or ClearBufferMask.DepthBufferBit Or ClearBufferMask.StencilBufferBit)

        GL.MatrixMode(MatrixMode.Modelview)
        MatriceView = Matrix4.CreateRotationY(cameraAngleY * DegToRad) * Matrix4.CreateRotationX(cameraAngleX * DegToRad) * Matrix4.CreateTranslation(0, 0, -cameraDistance)
        '// copy view matrix to OpenGL
        GL.LoadMatrix(MatriceView)
        'dessine la scéne mondiale vue de la caméra
        DessinerGrille()                        ' draw XZ-grid With Default size
        DessinerModel(0.7F, 1.0F, 1.0F, 0.5F)   ' draw model before transform

        ' compute model matrix. Dans ce cas le sens de la multiplication n'a pas d'importance puisque la rotation et la translation concerne le même axe
        MatriceModel = Matrix4.CreateRotationY(RotationModelY * DegToRad) * Matrix4.CreateTranslation(0.0F, 1.0F, 0.0F)
        'compute modelview matrix
        MatriceModelView = MatriceModel * MatriceView
        ' copy modelview matrix to OpenGL
        GL.LoadMatrix(MatriceModelView)
        'dessine les axes d'un modèle déplacé sur la scène mondiale avec la vue de la caméra
        DessinerAxes()
        'dessine un modèle déplacé sur la scène mondiale avec la vue de la caméra
        DessinerModel(1.0F, 1.0F, 1.0F, 1.0F)
        'ecrit les différentes informations 
        EcrireMatrices()
        'on affiche le tout à l'écran qu'on laisse à charge du rendu OpenGL
    End Sub
End Module