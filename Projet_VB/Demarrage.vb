Imports System.Threading

''' <summary> une seule sub en remplacement de l'initialisation de l'application par VB.
''' Correspondance directe avec C# </summary>
Friend Module Demarrage
    ''' <summary> Rassemble tout ce qui peut être mis en commun au niveau du projet </summary>
    Friend Const RadToDeg As Single = 180 / Math.PI
    Friend Const DegToRad As Single = Math.PI / 180
    Friend CrLf As String = Convert.ToChar(13) + Convert.ToChar(10)
    '''<summary>GUID unique par application, C'est celui de l'assembly générer à la création du projet. Voir Propriétés project 
    '''Permet de remplacer l'option Instance unique des projets VB en relation avec l'utilisation des Mutex</summary>
    Const AppID As String = "6e23f679-f78d-4959-a501-889eafe808ce"
    ''' <summary> Point d'entrée principal de l'application. </summary>
    <STAThread>
    Friend Sub Main()
        Using mutex As Mutex = New Mutex(False, AppID)
            If mutex.WaitOne(0) Then
                Call Application.EnableVisualStyles()
                Application.SetCompatibleTextRenderingDefault(False)
                Call Application.Run(New ChoixRendu())
            End If
        End Using
    End Sub
End Module