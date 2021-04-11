Imports System.Threading
Imports System.Threading.Tasks
''' <summary> formulaire qui contient le controlGL </summary>
Friend Class RenduControlGL_2
    Inherits Form
    'Pour l'émulation de l'évenement UpdateFrame
    Private Enum TypeUpdateFrame
        AUCUN = 0 : BOUCLE
    End Enum
    Private Const InitialTextFPS As String = "U:0 FPS, A:0 FPS, R:0 FPS"
    Private UpdateFrame As TypeUpdateFrame
    Private LastUpdateFrame As Date
    Private AnimateUpdate As Animation
    Private FPSUpdateFrame As Single
    Private TacheUpdateFrame As Task
    Private AnnulationTacheUpdateFrame As CancellationTokenSource
    Private AnimateRender As Animation
    Private FPSRenderFrame As Single
    Private FPSAnim, FPSAnimMax, FPSAnimMin As Single
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
        InitialiserRenduGL(FPSAnim, FPSAnimMax, FPSAnimMin)
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
            Case Keys.A
                FlagPaint = True
                If AnimateModel.IsAnimated Then AnimateModel.Arreter()
                If AnimateUpdate.IsAnimated Then
                    StoperBoucleUpdateFrame()
                End If
            Case Keys.B
                If UpdateFrame = TypeUpdateFrame.AUCUN Then
                    InitialiserBoucleUpdateFrame(TypeUpdateFrame.BOUCLE)
                End If
            Case Keys.L
                FlagPaint = True
                TailleLinePointPolygone += 1
                If TailleLinePointPolygone > 3 Then TailleLinePointPolygone = 1
            Case Keys.R
                If UpdateFrame = TypeUpdateFrame.AUCUN Then
                    FlagPaint = True
                    'il n'y a pas d'animation
                    RotationModelY += 3
                Else
                    If AnimateModel.IsAnimated Then
                        AnimateModel.Arreter()
                    Else
                        AnimateModel.Demarer()
                        RotationModelY += 1
                    End If
                End If
            Case Keys.F11
                If Me.WindowState = FormWindowState.Normal Then
                    Me.WindowState = FormWindowState.Maximized
                Else
                    Me.WindowState = FormWindowState.Normal
                End If
            Case Keys.Add
                AnimateModel.AugmenterFrequenceAnimation(1.0F)
            Case Keys.Subtract
                AnimateModel.DiminuerFrequenceAnimation(1.0F)
            Case Else
        End Select
        'on met à jour le dessin si il n'y a pas de boucle d'écoute
        If UpdateFrame = TypeUpdateFrame.AUCUN AndAlso FlagPaint Then
            RenduOpenGL.Invalidate()
        End If
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
        If UpdateFrame = TypeUpdateFrame.AUCUN AndAlso FlagPaint Then RenduOpenGL.Invalidate()
    End Sub

    Private Sub RenduOpenGL_MouseUp(sender As Object, e As Windows.Forms.MouseEventArgs)
    End Sub

    Private Sub RenduOpenGL_MouseWheel(sender As Object, e As Windows.Forms.MouseEventArgs)
    End Sub
#End Region
#Region "Emuler les évenements UpdateFrame et RenderFrame de la fenêtre GameWindows"
    'le controle n'a pas d'évenement UpdateFrame. Il faut en créer un à partir d'un timer ou autre
    Private Sub InitialiserBoucleUpdateFrame(ChoixUpdateFrame As TypeUpdateFrame)
        UpdateFrame = ChoixUpdateFrame
        TypeAnimation = [Enum].GetName(GetType(TypeUpdateFrame), UpdateFrame)
        TextFPS = InitialTextFPS
        If ChoixUpdateFrame = TypeUpdateFrame.BOUCLE Then
            'lance la boucle d'écoute
            AnnulationTacheUpdateFrame = New CancellationTokenSource()
            TacheUpdateFrame = Task.Factory.StartNew(Sub() LancerBoucleUpdateFrame(), AnnulationTacheUpdateFrame.Token)
        End If
    End Sub
    ''' <summary> Lance la boucle d'écoute pour l'évenement UpdateFrame sur un thread différent de celui des windowsform </summary>
    Private Sub LancerBoucleUpdateFrame()
        LastUpdateFrame = Date.Now
        AnimateRender = New Animation(FPSRenderFrame, FPSRenderFrame, FPSRenderFrame)
        AnimateRender.Demarer()
        AnimateUpdate = New Animation(FPSUpdateFrame, FPSUpdateFrame, FPSUpdateFrame)
        AnimateUpdate.Demarer()
        Do While Not AnnulationTacheUpdateFrame.IsCancellationRequested
            If InvokeRequired Then
                Invoke(New MethodInvoker(AddressOf RenduGL_UpdateFrame))
            End If
        Loop
    End Sub
    ''' <summary> arrête la boucle d'écoute pour l'évenement UpdateFrame </summary>
    Private Sub StoperBoucleUpdateFrame()
        If UpdateFrame = TypeUpdateFrame.BOUCLE Then
            'arrête la boucle d'écoute
            AnimateRender.Arreter()
            AnimateUpdate.Arreter()
            AnnulationTacheUpdateFrame.Cancel()
            Application.DoEvents()
            TacheUpdateFrame.Wait()
        End If
        InitialiserBoucleUpdateFrame(TypeUpdateFrame.AUCUN)
    End Sub
    ''' <summary> La signature ressemble à celle d'un autre évenement forms windows mais</summary>
    Private Sub RenduGL_UpdateFrame()
        Dim Temp As Date = Date.Now
        'durée entre 2 évènements UpdateFrame
        Dim Duree = (Temp - LastUpdateFrame).TotalSeconds
        LastUpdateFrame = Temp
        If AnimateUpdate.IsAnimation(Duree) Then
            'si il y a quelque chose à faire en particulier

        End If
        If AnimateModel.IsAnimated AndAlso AnimateModel.IsAnimation(Duree) Then
            'si il y a quelque chose à faire en particulier
            RotationModelY += 1
        End If
        If AnimateRender.IsAnimation(Duree) Then
            TextFPS = $"U:{AnimateUpdate.NbFPS} FPS, A:{AnimateModel.NbFPS} FPS, R:{AnimateRender.NbFPS} FPS"
            RenduOpenGL.Invalidate()
        End If
    End Sub
#End Region
#Region "Evenements formulaire"
    ''' <summary> on peut ajouter le Rendu OpenGL à ce niveau si on le souhaite </summary>
    Private Sub FormControlOpenTK_Load(sender As Object, e As EventArgs)
        'Dimension du formualire
        ClientSize = New Size(WidthInit, HeightInit)
        'création du rendu OpenGL
        RenduOpenGL = New GLControl(New GraphicsMode(), 3, 1, GraphicsContextFlags.Default) With {
            .Dock = DockStyle.Fill,             'Ici un seul control sur toute la surface client du formulaire
            .VSync = True                       'autre config concernant la qualité de l'affichage
        }
        'Ajout des évenements
        AjouterEvenementRenduOpenGL()
        'initialisation des FPS
        FPSUpdateFrame = 60
        FPSRenderFrame = 60
        FPSAnim = 30
        FPSAnimMax = FPSRenderFrame
        FPSAnimMin = 1
        'ajout du controle sur le formulaire
        Controls.Add(Me.RenduOpenGL)
        Dim glslSupported = GLExtensions.IsSupported("GL_ARB_shader_objects")
        'A partir d'ici le contexte OpenGL est initialisé on peut utiliser les fonctions et procédures OpenGL
        Exemple_Matrices_OpenTK()
        Exemple_Matrices_OpenGL()

        'lance le rappel pour l'émulation de l'évenement UpdateFrame pour l'animation. Doit être après l'ajout du RenduOpengl
        InitialiserBoucleUpdateFrame(TypeUpdateFrame.AUCUN)
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