''' <summary> formulaire qui contient le controlGL </summary>
Friend Class RenduControlGL
    Inherits Form
    'Pour l'émulation de l'évenement UpdateFrame
    Private Enum TypeUpdateFrame
        Aucun = 0 : IDLE : TIMER
    End Enum
    Private Const InitialTextFPS As String = "U:0 FPS, A:0 FPS, R:0 FPS"
    Private UpdateFrame As TypeUpdateFrame
    Private LastUpdateFrame As Date
    Private DureeFPSUpdateFrame As Double
    Private CptUpdateFrame, NbFPSFrame As Integer
    Private AnimateRender As FPS
    Private BoucleUpdateFrame As Timer
    'gère le déplacement automatique des modèles
    Private AnimateModel As FPS
    'pour l'affichage lié au dessin réalisé avec les commandes OpenGL
    Private RenduOpenGL As GLControl

    Private Sub AjouterEvenementRenduOpenGL()
        'Ajout des évenments du rendu OpenGL en remplacement du modificateur WithEvent
        AddHandler RenduOpenGL.Paint, New PaintEventHandler(AddressOf RenduOpenGL_Paint)
        AddHandler RenduOpenGL.Load, New EventHandler(AddressOf RenduOpenGL_Load)
        AddHandler RenduOpenGL.Resize, New EventHandler(AddressOf RenduOpenGL_Resize)
        AddHandler RenduOpenGL.KeyDown, New KeyEventHandler(AddressOf RenduOpenGL_KeyDown)
        AddHandler RenduOpenGL.KeyUp, New KeyEventHandler(AddressOf RenduOpenGL_KeyUp)
        AddHandler RenduOpenGL.PreviewKeyDown, New PreviewKeyDownEventHandler(AddressOf RenduOpenGL_PreviewKeyDown)
        AddHandler RenduOpenGL.MouseDown, New MouseEventHandler(AddressOf RenduOpenGL_MouseDown)
        AddHandler RenduOpenGL.MouseUp, New MouseEventHandler(AddressOf RenduOpenGL_MouseUp)
        AddHandler RenduOpenGL.MouseMove, New MouseEventHandler(AddressOf RenduOpenGL_MouseMove)
        AddHandler RenduOpenGL.MouseWheel, New MouseEventHandler(AddressOf RenduOpenGL_MouseWheel)
    End Sub
#Region "Evenements RenduGL"
    ''' <summary> L'initialisation d'openGL est faite uniquement à ce moment. On ne peut donc pas appeler de function OpenTK.GL
    '''  avant l'ajout du controle sur le formulaire même si le control est créé </summary>
    Private Sub RenduOpenGL_Load(sender As Object, e As EventArgs)
        InitialiserRenduGL()
    End Sub
    ''' <summary> remplace l'évènement RenderFrame de la GameWindow. Il faut le déclencher soit dans la boucle UpdateFrame
    ''' soit à chaque évenement du rendu Opengl qui modifie le dessin </summary>
    Private Sub RenduOpenGL_Paint(sender As Object, e As PaintEventArgs)
        DessinerScene()
        RenduOpenGL.SwapBuffers()
    End Sub
    ''' <summary> sert pour indiquer à OpenGL que le viewPort a changé ce qui modifie les matrices de projection </summary>
    Private Sub RenduOpenGL_Resize(sender As Object, e As EventArgs)
        SetViewPort(Me.ClientSize)
    End Sub
    ''' <summary> La gestion du clavier sous WindowsForms est différente de celle de la gameWindow 
    ''' Vous serez sans doute obligé de travailler sur les évenments PreviewKeyDown, KeyDown et keyUP</summary>
    Private Sub RenduOpenGL_PreviewKeyDown(sender As Object, e As PreviewKeyDownEventArgs)
        'Par exemple rend les touches de direction visibles sous KeyDown
        If e.KeyCode >= Keys.Left AndAlso e.KeyCode <= Keys.Down Then
            e.IsInputKey = True
        End If
    End Sub
    ''' <summary> La gestion du clavier sous WindowsForms est différente de celle de la gameWindow 
    ''' Vous serez sans doute obligé de travailler sur les évenments PreviewKeyDown, KeyDown et KeyUP</summary>
    Private Sub RenduOpenGL_KeyDown(sender As Object, e As KeyEventArgs)
        Dim FlagPaint As Boolean = False
        Select Case e.KeyCode'on prévoit toujours de pouvoir fermer le rendu OPENGL avec Escape
            Case Keys.Escape
                Close()
            Case Keys.Left To Keys.Down
                'Pour exemple. Ne fait rien
                Dim NumClavier = e.KeyValue
            Case Keys.F
                FlagPaint = True
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
            Case Keys.A, Keys.I, Keys.T
                FlagPaint = True
                If AnimateModel.IsStarted Then AnimateModel.Arreter()
                If UpdateFrame <> TypeUpdateFrame.Aucun Then StoperBoucleUpdateFrame()
                If e.KeyCode = Keys.I Then
                    LancerBoucleUpdateFrame(TypeUpdateFrame.IDLE)
                ElseIf e.KeyCode = Keys.T Then
                    LancerBoucleUpdateFrame(TypeUpdateFrame.TIMER)
                End If
            Case Keys.L
                FlagPaint = True
                TailleLinePointPolygone += 1
                If TailleLinePointPolygone > 3 Then TailleLinePointPolygone = 1
            Case Keys.R
                If UpdateFrame = TypeUpdateFrame.Aucun Then
                    FlagPaint = True
                    'il n'y a pas d'animation
                    RotationModelY += 3
                Else
                    If AnimateModel.IsStarted Then
                        AnimateModel.Arreter()
                    Else
                        AnimateModel.Demarer()
                        RotationModelY += 1
                        'If UpdateFrame = TypeUpdateFrame.TIMER Then RenduOpenGL.Invalidate()
                    End If
                End If
            Case Keys.F11
                If Me.WindowState = FormWindowState.Normal Then
                    Me.WindowState = FormWindowState.Maximized
                Else
                    Me.WindowState = FormWindowState.Normal
                End If
            Case Keys.P, Keys.Add
                AnimateModel.AugmenterFrequenceAnimation(1)
            Case Keys.M, Keys.Subtract
                AnimateModel.DiminuerFrequenceAnimation(1)
            Case Else
        End Select
        If UpdateFrame = TypeUpdateFrame.Aucun AndAlso FlagPaint Then RenduOpenGL.Invalidate()
    End Sub
    ''' <summary> La gestion du clavier sous WindowsForms est différente de celle de la gameWindow 
    ''' Vous serez sans doute obligé de travailler sur les évenments PreviewKeyDown, KeyDown et KeyUP</summary>
    Private Sub RenduOpenGL_KeyUp(sender As Object, e As KeyEventArgs)
    End Sub

    Private Sub RenduOpenGL_MouseDown(sender As Object, e As Windows.Forms.MouseEventArgs)
        If e.Button = MouseButtons.Left OrElse e.Button = MouseButtons.Right Then
            LastMouse = e.Location
        End If
    End Sub

    Private Sub RenduOpenGL_MouseMove(sender As Object, e As Windows.Forms.MouseEventArgs)
        Dim FlagPaint As Boolean = True
        If e.Button = MouseButtons.Left Then
            Dim DeltaX = e.X - LastMouse.X
            Dim DeltaY = e.Y - LastMouse.Y
            cameraAngleY += DeltaX * PasX
            cameraAngleX += DeltaY * PasY
            LastMouse = e.Location
        ElseIf e.Button = MouseButtons.Right Then
            Dim DeltaY = e.Y - LastMouse.Y
            cameraDistance -= DeltaY * PasDist
            LastMouse = e.Location
        Else
            FlagPaint = False
        End If
        If UpdateFrame = TypeUpdateFrame.Aucun AndAlso FlagPaint Then RenduOpenGL.Invalidate()
    End Sub

    Private Sub RenduOpenGL_MouseUp(sender As Object, e As Windows.Forms.MouseEventArgs)
    End Sub

    Private Sub RenduOpenGL_MouseWheel(sender As Object, e As Windows.Forms.MouseEventArgs)
    End Sub
#End Region
#Region "Emuler l'évenement UpdateFrame de la fenêtre GameWindows"
    'le controle n'a pas d'évenement UpdateFrame. Il faut en créer un à partir d'un timer ou autre
    Private Sub LancerBoucleUpdateFrame(ChoixUpdateFrame As TypeUpdateFrame)
        UpdateFrame = ChoixUpdateFrame
        EventFrame = [Enum].GetName(GetType(TypeUpdateFrame), UpdateFrame)
        TextFPS = InitialTextFPS
        If ChoixUpdateFrame = TypeUpdateFrame.Aucun Then Exit Sub
        If UpdateFrame = TypeUpdateFrame.TIMER Then
            ' On créer un timer avec un rappel le plus rapide possible mais en réalité on est limité par la mécanique interne du timer
            Dim Delai As Integer = 1000 \ 200
            BoucleUpdateFrame = New Timer With {.Interval = Delai}
            AddHandler BoucleUpdateFrame.Tick, New EventHandler(AddressOf RenduGL_UpdateFrame)
            BoucleUpdateFrame.Start()
        Else
            'on ajoute une écoute de l'évenement qui indique que l'application n'a plus de message à traiter
            AddHandler Application.Idle, AddressOf RenduGL_UpdateFrame
        End If
        'pour le 1er calcul de durée entre 2 rappels et pour les FPS
        LastUpdateFrame = Date.Now
        DureeFPSUpdateFrame = 0
        NbFPSFrame = 0
        CptUpdateFrame = 0
        'Pour simuler l'évenement Render de la GameWindows. 
        'Par du principe que l'evenement UpdateFrame se produit plus souvent que l'évènement Render
        AnimateRender = New FPS(60)
        AnimateRender.Demarer()
    End Sub
    Private Sub StoperBoucleUpdateFrame()
        If UpdateFrame = TypeUpdateFrame.TIMER Then
            BoucleUpdateFrame.Stop()
            BoucleUpdateFrame.Dispose()
            BoucleUpdateFrame = Nothing
        ElseIf UpdateFrame = TypeUpdateFrame.IDLE Then
            RemoveHandler Application.Idle, AddressOf RenduGL_UpdateFrame
        End If
        UpdateFrame = TypeUpdateFrame.Aucun
        EventFrame = [Enum].GetName(GetType(TypeUpdateFrame), UpdateFrame)
        TextFPS = InitialTextFPS
        AnimateRender = Nothing
    End Sub
    Private Sub RenduGL_UpdateFrame(Sender As Object, e As EventArgs)
        Dim Temp As Date = Date.Now
        'durée entre 2 évènements UpdateFrame
        Dim Duree = (Temp - LastUpdateFrame).TotalSeconds
        CptUpdateFrame += 1
        'stocke la durée depuis la dernière AAJ du FPS
        DureeFPSUpdateFrame += Duree
        If DureeFPSUpdateFrame > 1 Then
            DureeFPSUpdateFrame -= 1
            NbFPSFrame = CptUpdateFrame
            CptUpdateFrame = 0
        End If
        If AnimateModel.IsStarted AndAlso AnimateModel.IsDelaiFps(Duree) Then
            RotationModelY += 1
        End If

        If AnimateRender.IsDelaiFps(Duree) Then
            RenduOpenGL.Invalidate()
        Else
            If UpdateFrame = TypeUpdateFrame.IDLE Then
                EnvoyerMessageIDLE()
            End If
        End If
        TextFPS = $"U:{NbFPSFrame} FPS, A:{AnimateModel.FpsReel} FPS, R:{AnimateRender.FpsReel} FPS"
        LastUpdateFrame = Temp
    End Sub
    ''' <summary> permet d'envoyer un message dans la boucle d'écoute du formulaire afin de réactiver l'évenement IDLE.
    ''' le choix de SendKeys à la place de Cursor.Position est possible mais ralentit beaucoup. de 130 à 80 FPS </summary>
    Private Sub EnvoyerMessageIDLE()
        'envoi d'un message de déplacement de la souris
        'attention la souris doit être située sur la zone cliente de la fenêtre sinon cela ne fonctionne pas
        Dim P = Cursor.Position
        Cursor.Position = New Point(P.X + 1, P.Y - 1)
        P = Cursor.Position
        Cursor.Position = New Point(P.X - 1, P.Y + 1)
        'ou envoi d'une touche
        'attention la feêtre doit avoir le focus sinon cela ne fonctionne pas
        'SendKeys.Send("{F16}")
    End Sub
#End Region
#Region "Evenements formulaire"
    ''' <summary> on peut ajouter le Rendu OpenGL à ce niveau si on le souhaite </summary>
    Private Sub FormControlOpenTK_Load(sender As Object, e As EventArgs)
        'Dimension du formualire
        ClientSize = New Size(WidthInit, HeightInit)
        AnimateModel = New FPS(60, 120, 30)
        'création du rendu OpenGL
        RenduOpenGL = New GLControl(New GraphicsMode(), 3, 1, GraphicsContextFlags.Default) With {
            .Dock = DockStyle.Fill,             'Ici un seul control sur toute la surface client du formulaire
            .VSync = True                       'autre config concernant la qualité de l'affichage
        }
        'Ajout des évenements
        AjouterEvenementRenduOpenGL()
        'ajout du controle sur le formulaire
        Controls.Add(Me.RenduOpenGL)

        'A partir d'ici le contexte OpenGL est initialisé on peut utiliser les fonctions et procédures OpenGL
        'Exemple_Matrices_OpenGl()

        'lance le rappel pour l'émulation de l'évenement UpdateFrame pour l'animation. Doit être après l'ajout du RenduOpengl
        LancerBoucleUpdateFrame(TypeUpdateFrame.Aucun)
        Text = "OpenTK & VB & Net 4.8 : OpenGL Version: " & GL.GetString(StringName.Version) & ", Rendu : " & GL.GetString(StringName.Renderer)
        Cursor.Position = PointToScreen(New Point(WidthInit \ 2, HeightInit \ 2))
    End Sub
    ''' <summary> Interdit un redimensionnement trop petit du formulaire
    ''' uniquement pour faire la correspondance avec la GameWindows car il existe propriété pour celà </summary>
    Private Sub FormControlOpenTK_Resize(sender As Object, e As EventArgs)
        Dim W1 = Me.ClientSize.Width
        Dim H1 = Me.ClientSize.Height
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
            Me.ClientSize = New Size(W1, H1)
        End If
    End Sub
    ''' <summary> GL_Control n'a pas d'évenement Close ou UnLoad c'est donc ici qu'il faut gérer la libération des ressources non managées </summary>
    Private Sub FormControlOpenTK_Closing(sender As Object, e As FormClosingEventArgs)
    End Sub
    ''' <summary> GL_Control n'a pas d'évenement Close ou UnLoad c'est donc ici qu'il faut gérer la libération des ressources non managées </summary>
    Private Sub FormControlOpenTK_Closed(sender As Object, e As FormClosedEventArgs)
        StoperBoucleUpdateFrame()
    End Sub
#End Region
End Class