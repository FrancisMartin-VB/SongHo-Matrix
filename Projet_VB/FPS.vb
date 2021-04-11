''' <summary> Permet de renvoyer true si un délai est arrivé à terme ou dépassé.
''' la précision n'est pas très bonne mais tout à fait correcte dans le temps 
''' remplace avantageusement une minuterie dans une boucle continue </summary>
Friend Class FPS
    Private DelaiFps As Double
    Private Fps As Integer
    Private ReadOnly MaxFps As Integer
    Private ReadOnly MinFps As Integer
    'temps écoulé depuis le lancement de l'animation en secondes
    Private TempsEcoule As Double
    Private LastFPS As Double
    Private CptFPS As Integer

    ''' <summary> NbFPS compter pendant la dernière seconde </summary>
    Friend ReadOnly Property FpsReel As Integer
    ''' <summary> flag indiquant si le compteur de FPS est démaré</summary>
    Friend ReadOnly Property IsStarted As Boolean

    ''' <summary> démarre l'animation. L'appel de IsAnimation renvoie toujours True si le delai d'animation est atteint </summary>
    Friend Sub Demarer()
        _IsStarted = True
        TempsEcoule = 0
        CptFPS = 0
        LastFPS = 0
        DelaiFps = (1 / Fps)
    End Sub
    ''' <summary> arrête l'animation. L'appel de IsAnimation renvoie toujours False </summary>
    Friend Sub Arreter()
        _IsStarted = False
        _FpsReel = 0
    End Sub
    ''' <summary> indique si le délai demandé pour le FPS en cours est atteint ou dépassé 
    ''' La fréquence d'appel de cette fonction doit être plus élevée ou égale que celle du FPS demandé </summary>
    ''' <param name="DeltaSeconde"> nb de seconde depuis l'appel précédent</param>
    Friend Function IsDelaiFps(DeltaSeconde As Double) As Boolean
        LastFPS += DeltaSeconde
        If LastFPS >= 1 Then
            _FpsReel = CptFPS
            CptFPS = 0
            LastFPS -= 1
        End If
        TempsEcoule += DeltaSeconde
        If TempsEcoule >= DelaiFps Then
            TempsEcoule -= DelaiFps
            CptFPS += 1
            Return True
        End If
        Return False
    End Function
    ''' <summary> augmente la fréquence d'animation </summary>
    ''' <param name="NbFpsPlus"> Nb d'animations par seconde à ajouter à la fréquence actuelle </param>
    Friend Sub AugmenterFrequenceAnimation(NbFpsPlus As Integer)
        If Fps + NbFpsPlus > MaxFps Then
            Fps = MaxFps
        Else
            Fps += NbFpsPlus
        End If
        DelaiFps = (1 / Fps)
    End Sub
    ''' <summary> diminue la fréquence d'animation </summary>
    ''' <param name="NbFpsMoins"> Nb d'animations par seconde à enlever à la fréquence actuelle </param>
    Friend Sub DiminuerFrequenceAnimation(NbFpsMoins As Integer)
        If Fps - NbFpsMoins < MinFps Then
            Fps = MinFps
        Else
            Fps -= NbFpsMoins
        End If
        DelaiFps = (1 / Fps)
    End Sub
    ''' <summary> permet de gérer une animation </summary>
    ''' <param name="NbFps"> nb initial d'animations par seconde </param>
    Friend Sub New(Optional NbFps As Integer = 30, Optional FpsMax As Integer = 60, Optional FpsMin As Integer = 1)
        MaxFps = FpsMax
        MinFps = FpsMin
        If NbFps < MinFps OrElse
           NbFps > MaxFps OrElse
           NbFps <= 0 Then
            Throw New Exception("Delai Animation out of range")
        End If
        Me.Fps = NbFps
    End Sub
End Class