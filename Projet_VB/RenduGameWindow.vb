Friend Class RenduGameWindow
    'Pour l'évenement UpdateFrame
    Private Enum TypeUpdateFrame
        GAME = 0 : Aucun
    End Enum
    Private UpdateFrame As TypeUpdateFrame
    Private LastUpdateFrame As Date
    Private LastUpdateRender As Date
    Private LastFPSFrame, LastFPSRender As Double
    Private CptFPSFrame, NbFPSFrame, CptFPSRender, NbFPSRender As Integer

    'gère le déplacement automatique des modèles
    Private AnimateModel As FPS
    Private RenduOpenGL As GameWindow
    ''' <summary> Initialisation du rendu OpenGL et de la machine Etat OpenGL</summary>
    ''' <param name="Update"> FPS souhaité pour l'évenement Update Frame </param>
    ''' <param name="Render"> FPS souhaité pour l'événement Render Frame </param>
    Friend Sub New(Optional Update As Integer = 120, Optional Render As Integer = 60)
        'Initialisation polyvalente 
        'RenduOpenGL = New GameWindow(WidthInit, HeightInit)

        'Initialisation permettant d'affiner le contexte d'éxécution de la fenêtre de rendu. Surtout utile car il permet de choisir l'écran
        'RenduOpenGL = New GameWindow(WidthInit, HeightInit, New GraphicsMode(), "OpenTK & VB & Net 4.8 ", GameWindowFlags.Default, DisplayDevice.GetDisplay(DisplayIndex.Second))

        'Initialisation permettant d'affiner le contexte d'éxécution de la fenêtre de rendu. Surtout utile car il permet de choisir la version d'OpenGL
        'par exemple ce programme n'affichera rien si on met 3.2 au lieu de 3.1 car une grande partie des sub de dessin ont été dépréciées avec la version 3.2 (OpenGL moderne)
        RenduOpenGL = New GameWindow(WidthInit, HeightInit, New GraphicsMode(), "OpenTK & VB & Net 4.8 ", GameWindowFlags.Default,
                                     DisplayDevice.GetDisplay(DisplayIndex.Second), 3, 1, GraphicsContextFlags.Default)

        'A partir d'ici le contexte OpenGL est initialisé on peut utiliser les fonctions et procédures OpenGL
        Exemple_Matrices_OpenTK()
        Exemple_Matrices_OpenGL()

        RenduOpenGL.Title &= " : OpenGL Version: " & GL.GetString(StringName.Version) & ", Rendu : " & GL.GetString(StringName.Renderer)
        RenduOpenGL.VSync = VSyncMode.On
        RenduOpenGL.CursorVisible = True
        RenduOpenGL.WindowBorder = WindowBorder.Resizable
        AjouterEvenementRenduOpenGL()

        'initialisation des diverses variables prises en compte pour le dessin de la sc
        AnimateModel = New FPS(Render, Update, Render \ 2)
        EventFrame = [Enum].GetName(GetType(TypeUpdateFrame), UpdateFrame)
        LastUpdateFrame = Date.Now
        LastFPSFrame = 0
        NbFPSFrame = 0
        CptFPSFrame = 0
        LastUpdateRender = Date.Now
        LastFPSRender = 0
        NbFPSRender = 0
        CptFPSRender = 0
        Dim P As Point = RenduOpenGL.PointToScreen(New Point(WidthInit \ 2, HeightInit \ 2))
        Mouse.SetPosition(P.X, P.Y)
        'ne pas spécifier Upadate et Render pour avoir le max possible du système 
        RenduOpenGL.Run(Update, Render)
    End Sub
    Private Sub AjouterEvenementRenduOpenGL()
        'evenements gérés associés au rendu OPENGL
        AddHandler RenduOpenGL.Load, AddressOf RenduGL_Load
        AddHandler RenduOpenGL.UpdateFrame, AddressOf RenduGL_UpdateFrame
        AddHandler RenduOpenGL.RenderFrame, AddressOf RenduGL_RenderFrame
        AddHandler RenduOpenGL.Resize, AddressOf RenduGL_Resize
        AddHandler RenduOpenGL.KeyDown, AddressOf RenduGL_KeysDown
        AddHandler RenduOpenGL.KeyUp, AddressOf RenduGL_KeysUp
        AddHandler RenduOpenGL.MouseMove, AddressOf RenduGL_MouseMove
        AddHandler RenduOpenGL.MouseWheel, AddressOf RenduGL_MouseWheel
        AddHandler RenduOpenGL.MouseDown, AddressOf RenduGL_MouseDown
        AddHandler RenduOpenGL.MouseUp, AddressOf RenduGL_MouseUp
        AddHandler RenduOpenGL.Closed, AddressOf RenduGL_Closed
        AddHandler RenduOpenGL.Unload, AddressOf RenduGL_Unload
    End Sub
    ''' <summary> initialisation des états d'OPENGL. 
    ''' C'est là notament que l'on retrouve les activations des fonctions blend, depth, texture, alpha, etc...</summary> 
    Private Sub RenduGL_Load(ByVal sender As Object, ByVal e As EventArgs)
        InitialiserRenduGL(RenduOpenGL.Context)
    End Sub
    ''' <summary> On desinstalle les ressources impliquées sur la carte vidéo </summary>
    Private Sub RenduGL_Unload(ByVal sender As Object, ByVal e As EventArgs)
    End Sub
    ''' <summary> On desinstalle les ressources impliquées sur la carte vidéo </summary>
    Private Sub RenduGL_Closed(ByVal sender As Object, ByVal e As EventArgs)
    End Sub
    Private Sub RenduGL_MouseDown(ByVal sender As Object, ByVal e As MouseButtonEventArgs)
        If e.Mouse.LeftButton = Input.ButtonState.Pressed OrElse e.Mouse.RightButton = Input.ButtonState.Pressed Then
            LastMouse = e.Position
        End If
    End Sub
    Private Sub RenduGL_MouseUp(ByVal sender As Object, ByVal e As MouseButtonEventArgs)
    End Sub
    Private Sub RenduGL_MouseWheel(ByVal sender As Object, ByVal e As MouseWheelEventArgs)
    End Sub
    Private Sub RenduGL_MouseMove(ByVal sender As Object, ByVal e As MouseMoveEventArgs)
        If e.Mouse.LeftButton = Input.ButtonState.Pressed Then
            Dim DeltaX = e.X - LastMouse.X
            Dim DeltaY = e.Y - LastMouse.Y
            cameraAngleY += DeltaX * PasX
            cameraAngleX += DeltaY * PasY
            LastMouse = e.Position
        ElseIf e.Mouse.RightButton = Input.ButtonState.Pressed Then
            Dim DeltaY = e.Y - LastMouse.Y
            cameraDistance -= DeltaY * PasDist
            LastMouse = e.Position
        End If
    End Sub
    ''' <summary> gère les entrées utilisateurs au clavier </summary>
    Private Sub RenduGL_KeysDown(ByVal sender As Object, e As KeyboardKeyEventArgs)
        Debug.Print($"KeysDown : {e.Key} - {sender}")
        Select Case e.Key'on prévoit toujours de pouvoir fermer le rendu OPENGL avec Escape
            Case Key.Escape
                RenduOpenGL.Close()
            Case Key.F
                If Rendu = PolygonMode.Fill Then
                    Rendu = PolygonMode.Line
                    GL.Disable(EnableCap.DepthTest)
                    GL.Disable(EnableCap.CullFace)
                ElseIf Rendu = PolygonMode.Line Then
                    Rendu = PolygonMode.Point
                    GL.Disable(EnableCap.DepthTest)
                    GL.Disable(EnableCap.CullFace)
                Else
                    Rendu = PolygonMode.Fill
                    GL.Enable(EnableCap.DepthTest)
                    GL.Enable(EnableCap.CullFace)
                End If
                GL.PolygonMode(MaterialFace.FrontAndBack, Rendu)
            Case Key.L
                TailleLinePointPolygone += 1
                If TailleLinePointPolygone > 3 Then TailleLinePointPolygone = 1
            Case Key.Q, Key.A, Key.G
                'on arrête l'animation si nécessaire
                If AnimateModel.IsStarted Then AnimateModel.Arreter()
                If e.Key = Key.G Then
                    UpdateFrame = TypeUpdateFrame.GAME
                Else
                    UpdateFrame = TypeUpdateFrame.Aucun
                End If
                EventFrame = [Enum].GetName(GetType(TypeUpdateFrame), UpdateFrame)
            Case Key.R
                If UpdateFrame = TypeUpdateFrame.GAME Then
                    If AnimateModel.IsStarted Then
                        AnimateModel.Arreter()
                    Else
                        AnimateModel.Demarer()
                        RotationModelY += 1
                    End If
                Else
                    RotationModelY += 3
                End If
            Case Key.F11
                If RenduOpenGL.WindowState = WindowState.Normal Then
                    RenduOpenGL.WindowState = WindowState.Maximized
                ElseIf RenduOpenGL.WindowState = WindowState.Maximized Then
                    RenduOpenGL.WindowState = WindowState.Fullscreen
                Else
                    RenduOpenGL.WindowState = WindowState.Normal
                End If
            Case Key.P, Key.KeypadPlus
                AnimateModel.AugmenterFrequenceAnimation(1)
            Case Key.Semicolon, Key.KeypadSubtract
                AnimateModel.DiminuerFrequenceAnimation(1)
            Case Else
        End Select
    End Sub
    ''' <summary> gère les entrèes utilisateurs au clavier </summary>
    Private Sub RenduGL_KeysUp(ByVal sender As Object, e As KeyboardKeyEventArgs)
    End Sub
    ''' <summary> chaque fois que les dimensions de la fenêtre changent</summary>
    Private Sub RenduGL_Resize(ByVal sender As Object, ByVal e As EventArgs)
        Console.WriteLine($"taille RenduGL : {RenduOpenGL.ClientSize}, Etat : {RenduOpenGL.WindowState}")
        Dim W1 = RenduOpenGL.ClientSize.Width
        Dim H1 = RenduOpenGL.ClientSize.Height
        Dim Flag As Boolean
        If W1 < 250 Then
            W1 = 250
            Flag = True
        End If
        If H1 < 400 Then
            H1 = 400
            Flag = True
        End If
        If Flag Then
            RenduOpenGL.ClientSize = New Size(W1, H1)
            Exit Sub
        End If
        SetViewPort(RenduOpenGL.ClientSize)
    End Sub
    ''' <summary> à chaque évenement autre que Paint </summary>
    Private Sub RenduGL_UpdateFrame(ByVal sender As Object, ByVal e As FrameEventArgs)
        Dim Temp As Date = Date.Now
        'durée entre 2 évènements 
        Dim Duree = (Temp - LastUpdateFrame).TotalSeconds
        CptFPSFrame += 1
        'stocke la durée depuis la dernière MAJ du FPS
        LastFPSFrame += Duree
        If LastFPSFrame > 1 Then
            LastFPSFrame -= 1
            NbFPSFrame = CptFPSFrame
            CptFPSFrame = 0
        End If
        If AnimateModel.IsStarted AndAlso AnimateModel.IsDelaiFps(Duree) Then
            RotationModelY += 1
        End If
        TextFPS = $"U:{NbFPSFrame} FPS, A:{AnimateModel.FpsReel} FPS, R:{NbFPSRender} FPS"
        LastUpdateFrame = Temp
    End Sub
    ''' <summary> dessin à effectuer à chaque mise à jour automatique (minuterie) de l'écran ou chaque fois que possible si la fréquence demandée est trop rapide </summary> 
    Private Sub RenduGL_RenderFrame(ByVal sender As Object, ByVal e As FrameEventArgs)
        Dim Temp As Date = Date.Now
        'durée entre 2 évènements 
        Dim Duree = (Temp - LastUpdateRender).TotalSeconds
        CptFPSRender += 1
        'stocke la durée depuis la dernière MAJ du FPS
        LastFPSRender += Duree
        If LastFPSRender > 1 Then
            LastFPSRender -= 1
            NbFPSRender = CptFPSRender
            CptFPSRender = 0
        End If
        LastUpdateRender = Temp
        DessinerScene()
    End Sub
End Class