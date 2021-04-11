Imports System.Threading
Imports System.Threading.Tasks
''' <summary> formulaire qui contient le controlGL. 
''' Le ControlGL n'a pas les évenements UpdateFrame et RenderFrame qui sont générés par la boucle de jeux de la GameWindows
''' Vous pouvez les émuler en appelant la procédure GererBoucleUpdateFrame et indiquer si vous voulez une boucle d'écoute spécifique pour le RenderFrame </summary>
Friend Class RenduControlGL_2
    Inherits Form
#Region "Champs"
    'variables et autres pour l'aiguillage du type d'EventFrame
    Private _EventsFrame As TypeEventsFrame
    Private Property EventsFrame As TypeEventsFrame
        Get
            Return _EventsFrame
        End Get
        Set(value As TypeEventsFrame)
            _EventsFrame = value
            If _EventsFrame = TypeEventsFrame.AUCUN Then
                EventFrame = "AUCUN"
                TextFPS = "U:0 FPS, A:0 FPS, R:0 FPS"
            Else
                EventFrame = "BOUCLE"
                If BoucleRenderFrame Then EventFrame &= "S"
                'TextFPS est mis à jour à chaque évenement RenderFrame
            End If
        End Set
    End Property
    Private Enum TypeEventsFrame
        AUCUN = 0 : BOUCLE
    End Enum

    'Variables pour la gestion de l'émulation des évenements UpdateFrame et RenderFrame
    Private DetruireEventsFrame As CancellationTokenSource
    Private BoucleRenderFrame As Boolean
    Private LastUpdateFrame As Date
    Private EventUpdateFrame As FPS
    Private FpsEventUpdateFrame As Integer
    Private CreerEventUpdateFrame As Task
    Private LastRenderFrame As Date
    Private EventRenderFrame As FPS
    Private FpsEventRenderFrame As Integer
    Private CreerEventRenderFrame As Task

    'gère le déplacement automatique des modèles dans le cas d'une boucle d'écoute
    Private EventAnimateModel As FPS

    'pour l'affichage lié au dessin réalisé avec les commandes OpenGL
    Private RenduOpenGL As GLControl
#End Region
#Region "Evenements RenduOpenGL"
    ''' <summary> Ajout du gestionaire d'évenements pour le Rendu OpenGL. 
    ''' on peut enlever ceux qui sont inutiles </summary>
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
    'généralement on doit supprimer dans le gestionnaire l'écoute des événements non utilisés
    ''' <summary> L'initialisation d'openGL est faite uniquement à ce moment. On ne peut donc pas appeler de function OpenTK.GL
    '''  avant l'ajout du controle sur le formulaire même si le control est créé </summary>
    Private Sub RenduOpenGL_Load(sender As Object, e As EventArgs)
        InitialiserRenduGL()
    End Sub
    ''' <summary> Il faut le déclencher pour des mises à jour autres que la mise à jour du Rendu OpenGL </summary>
    Private Sub RenduOpenGL_Paint(sender As Object, e As PaintEventArgs)
    End Sub
    ''' <summary> sert pour indiquer à OpenGL que le viewPort a changé ce qui modifie les matrices de projection </summary>
    Private Sub RenduOpenGL_Resize(sender As Object, e As EventArgs)
        SetViewPort(Me.ClientSize)
        If EventsFrame = TypeEventsFrame.AUCUN Then RenduOpenGL_Paint()
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
        Debug.Print($"Touche Code : {e.KeyCode}, Value : {e.KeyValue}, Data : {e.KeyData}")
        Dim FlagPaint As Boolean = False ' mettre à true sur les actions qui modifie la scène
        Select Case e.KeyCode'on prévoit toujours de pouvoir fermer le rendu OPENGL avec Escape
            Case Keys.Escape
                Close()
            Case Keys.Left To Keys.Down
                'Pour l'exemple de PreviewKeyDown. Ne fait rien
                Dim NumClavier = e.KeyValue
            Case Keys.F
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
                'on demande la mise à jour de la scène
                FlagPaint = EventsFrame = TypeEventsFrame.AUCUN
            Case Keys.A
                If EventsFrame = TypeEventsFrame.BOUCLE Then
                    GererBoucleEventsFrame(TypeEventsFrame.AUCUN)
                    'on demande la mise à jour de la scène
                    FlagPaint = True
                End If
            Case Keys.B
                If EventsFrame = TypeEventsFrame.AUCUN Then
                    If e.Control Then
                        GererBoucleEventsFrame(TypeEventsFrame.BOUCLE, True)
                    Else
                        GererBoucleEventsFrame(TypeEventsFrame.BOUCLE, False)
                    End If
                End If
            Case Keys.L
                TailleLinePointPolygone += 1
                If TailleLinePointPolygone > 3 Then TailleLinePointPolygone = 1
                'on demande la mise à jour de la scène
                FlagPaint = EventsFrame = TypeEventsFrame.AUCUN
            Case Keys.R
                If EventsFrame = TypeEventsFrame.AUCUN Then
                    'il n'y a pas d'animation
                    RotationModelY += 3
                    'on demande la mise à jour de la scène
                    FlagPaint = True
                Else
                    If EventAnimateModel.IsStarted Then
                        EventAnimateModel.Arreter()
                    Else
                        EventAnimateModel.Demarer()
                        RotationModelY += 1
                    End If
                End If
            Case Keys.F11
                If Me.WindowState = FormWindowState.Normal Then
                    Me.WindowState = FormWindowState.Maximized
                Else
                    Me.WindowState = FormWindowState.Normal
                End If
            Case Keys.P, Keys.Add
                If EventAnimateModel.IsStarted Then EventAnimateModel.AugmenterFrequenceAnimation(1)
            Case Keys.M, Keys.Subtract
                If EventAnimateModel.IsStarted Then EventAnimateModel.DiminuerFrequenceAnimation(1)
            Case Else
        End Select
        'on met à jour le dessin si il n'y a pas de boucle d'écoute
        If FlagPaint Then RenduOpenGL_Paint()
    End Sub
    ''' <summary> La gestion du clavier sous WindowsForms est différente de celle de la gameWindow 
    ''' Vous serez sans doute obligé de travailler sur les évenments PreviewKeyDown, KeyDown et KeyUP</summary>
    Private Sub RenduOpenGL_KeyUp(sender As Object, e As KeyEventArgs)
    End Sub
    ''' <summary> indique qu'un bouton de la souris vient d'être appuyé </summary>
    Private Sub RenduOpenGL_MouseDown(sender As Object, e As Windows.Forms.MouseEventArgs)
        If e.Button = MouseButtons.Left OrElse e.Button = MouseButtons.Right Then
            LastMouse = e.Location
        End If
    End Sub
    ''' <summary> indique que la souris vient d'être déplacée </summary>
    Private Sub RenduOpenGL_MouseMove(sender As Object, e As Windows.Forms.MouseEventArgs)
        Dim FlagPaint As Boolean = EventsFrame = TypeEventsFrame.AUCUN
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
        End If
        If FlagPaint Then RenduOpenGL_Paint() 'RenduOpenGL.Invalidate()
    End Sub
    ''' <summary> indique qu'un bouton de la souris vient d'être appuyé </summary>
    Private Sub RenduOpenGL_MouseUp(sender As Object, e As Windows.Forms.MouseEventArgs)
    End Sub
    ''' <summary> indique que la molette de la souris vient d'être tournée </summary>
    Private Sub RenduOpenGL_MouseWheel(sender As Object, e As Windows.Forms.MouseEventArgs)
    End Sub
#End Region
#Region "Emuler les évenements UpdateFrame et RenderFrame de la fenêtre GameWindows"
    ''' <summary> gère la ou les boucles d'écoute pour simuler les évènements UpdateFrame et RenderFrame de la GameWindows </summary>
    ''' <param name="ChoixUpdateFrame"> les évenements Upate et Render Frame sont activés ou pas </param>
    ''' <param name="RenderFrame"> indique que l'évènement RenderFrame à sa propre boucle. consomme un thread en plus </param>
    Private Sub GererBoucleEventsFrame(ChoixUpdateFrame As TypeEventsFrame, Optional RenderFrame As Boolean = False)
        If ChoixUpdateFrame = TypeEventsFrame.BOUCLE Then
            If EventsFrame = TypeEventsFrame.AUCUN Then
                'on interdit les maj de dessin
                BoucleRenderFrame = RenderFrame
                EventsFrame = TypeEventsFrame.BOUCLE
                'lance la boucle d'écoute des événements
                DetruireEventsFrame = New CancellationTokenSource()
                CreerEventUpdateFrame = Task.Factory.StartNew(Sub() RenduOpenGL_UpdateFrame(), DetruireEventsFrame.Token)
                If BoucleRenderFrame Then 'on créer une boucle d'écoute spécifique
                    CreerEventRenderFrame = Task.Factory.StartNew(Sub() RenduOpenGL_RenderFrame(), DetruireEventsFrame.Token)
                End If
            End If
        Else
            If EventsFrame = TypeEventsFrame.BOUCLE Then
                'destruction des boucles d'écoute
                DetruireEventsFrame.Cancel()
                'on finit tous les mesages de la boucle d'écoute du formulaire
                Application.DoEvents()
                'on attend l'arrêt effectif des boucles d'écoutes
                If BoucleRenderFrame Then
                    Task.WaitAll(CreerEventUpdateFrame, CreerEventRenderFrame)
                Else
                    CreerEventUpdateFrame.Wait()
                End If
            End If
            EventsFrame = TypeEventsFrame.AUCUN
        End If
    End Sub
    ''' <summary> Lance la boucle d'écoute pour l'évenement RenderFrame sur un thread différent de celui des windowsform </summary>
    Private Sub RenduOpenGL_RenderFrame()
        LastRenderFrame = Date.Now
        EventRenderFrame.Demarer()
        Do
            Dim Temp As Date = Date.Now
            'durée entre 2 évènements UpdateFrame
            Dim Duree = (Temp - LastRenderFrame).TotalSeconds
            LastRenderFrame = Temp
            'cet appel doit normalement être celui qui prends le plus de temps 
            'on peut si le FPS est Crucial lancer une deuxième boucle uniquement pour le RenderFrame
            If EventRenderFrame.IsDelaiFps(Duree) Then
                'obligatoire car on n'a pas accès au contexte opengl du control donc on perd du temps de synchro de threads
                'mais on by-pass complètement les windows-form. un seul thread est utilisé pour le dessin
                Invoke(New MethodInvoker(AddressOf RenduOpenGL_Paint))
            End If
        Loop Until DetruireEventsFrame.IsCancellationRequested
        EventRenderFrame.Arreter()
    End Sub
    ''' <summary> Lance la boucle d'écoute pour l'évenement UpdateFrame sur un thread différent de celui des windowsform </summary>
    Private Sub RenduOpenGL_UpdateFrame()
        LastUpdateFrame = Date.Now
        'si le renderframe n'a pas sa propre boucle
        If Not BoucleRenderFrame Then EventRenderFrame.Demarer()
        EventUpdateFrame.Demarer()
        Do
            Dim Temp As Date = Date.Now
            'durée entre 2 évènements UpdateFrame
            Dim Duree = (Temp - LastUpdateFrame).TotalSeconds
            LastUpdateFrame = Temp
            If EventUpdateFrame.IsDelaiFps(Duree) Then
                'si il y a quelque chose à faire en particulier
            End If
            If EventAnimateModel.IsStarted AndAlso EventAnimateModel.IsDelaiFps(Duree) Then
                RotationModelY += 1
            End If
            'l'appel à AnimateRender doit normalement être celui qui prend le plus de temps car il dessine la scène
            'on peut si le FPS UpdateFrame et AnimateModel est crucial lancer une deuxième boucle uniquement pour le RenderFrame
            'sinon il ralentira EventUpdateFrame si le dessin prend trop de temps. Attention la propriété VSync à True du RenduOpenGL
            'limite de fait les FPS à la vitesse de rafraichissement d'écran la plus élevée de votre système.
            If Not BoucleRenderFrame AndAlso EventRenderFrame.IsDelaiFps(Duree) Then
                'Invoke obligatoire car on n'a pas accès au contexte opengl du control donc on perd du temps de synchro de threads
                'mais on by-pass complètement les windows-form. un seul thread est utilisé pour le dessin
                Invoke(New MethodInvoker(AddressOf RenduOpenGL_Paint))
            End If
        Loop Until DetruireEventsFrame.IsCancellationRequested
        EventAnimateModel.Arreter()
        'si le renderframe n'a pas sa propre boucle
        If Not BoucleRenderFrame Then EventRenderFrame.Arreter()
        EventUpdateFrame.Arreter()
    End Sub
    ''' <summary> procédure pour la mise à jour du dessin sur le rendu OpenGL. 
    ''' On By-pass les évenements WindowsForms pour éviter de perdre trop de temps mais il faut quand même que 
    ''' le thread windows form soit synchro d'où un appel par invoke si l'appel à lieu en dehors du thread WinowsForm</summary>
    Private Sub RenduOpenGL_Paint()
        'calcul le texte à afficher concernant les FPS
        TextFPS = $"U:{EventUpdateFrame.FpsReel} FPS, A:{EventAnimateModel.FpsReel} FPS, R:{EventRenderFrame.FpsReel} FPS"
        DessinerScene()
        RenduOpenGL.SwapBuffers()
    End Sub
#End Region
#Region "Evenements formulaire"
    ''' <summary> on peut ajouter le Rendu OpenGL à ce niveau si on le souhaite </summary>
    Private Sub FormControlOpenTK_Load(sender As Object, e As EventArgs)
        'Dimensions initiales du formulaire
        ClientSize = New Size(WidthInit, HeightInit)
        'initialisation des FPS désirés si on lance la boucle d'écoute
        FpsEventUpdateFrame = 120
        EventUpdateFrame = New FPS(FpsEventUpdateFrame, FpsEventUpdateFrame, FpsEventUpdateFrame)
        FpsEventRenderFrame = 60
        EventRenderFrame = New FPS(FpsEventRenderFrame, FpsEventRenderFrame, FpsEventRenderFrame)
        EventAnimateModel = New FPS(FpsEventRenderFrame, FpsEventUpdateFrame, FpsEventRenderFrame \ 2)
        'création du rendu OpenGL
        RenduOpenGL = New GLControl(New GraphicsMode(), 3, 1, GraphicsContextFlags.Default) With {
            .Dock = DockStyle.Fill,'Ici un seul control sur toute la surface client du formulaire
            .VSync = True          'autre config concernant la qualité de l'affichage. Attention limite le FPS à celui de l'écran
        }
        'Ajout des évenements du rendu OpenGL
        AjouterEvenementRenduOpenGL()
        'ajout du controle  de rendu sur le formulaire
        Controls.Add(RenduOpenGL)
        'initialise la boucle d'écoute pour l'émulation des évenements UpdateFrame et RenderFrame.
        GererBoucleEventsFrame(TypeEventsFrame.BOUCLE)

        'A partir d'ici le contexte OpenGL est initialisé on peut utiliser les fonctions et procédures OpenGL si besoin
        Exemple_Matrices_OpenTK()
        Exemple_Matrices_OpenGL()

        Text = "OpenTK & VB & Net 4.8 : OpenGL Version: " & VersionOpenGL
        Cursor.Position = PointToScreen(New Point(WidthInit \ 2, HeightInit \ 2))
    End Sub
    ''' <summary> Empêche un redimensionnement trop petit du formulaire </summary>
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
        'on détruit la boucle d'écoute si elle existe encore
        If EventsFrame = TypeEventsFrame.BOUCLE Then
            GererBoucleEventsFrame(TypeEventsFrame.AUCUN)
        End If
    End Sub
#End Region
End Class