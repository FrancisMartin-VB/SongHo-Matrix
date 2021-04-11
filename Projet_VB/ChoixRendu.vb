Friend Class ChoixRendu
    Inherits Form
    'déclaration d'une sub normale sans la clause Handles.
    Private Sub Form1_Load(sender As Object, e As EventArgs) 'Handles MyBase.Load
    End Sub
    'déclaration d'une sub normale sans la clause Handles.
    Private Sub Button1_Click(sender As Object, e As EventArgs) 'Handles Button1.Click
        Hide()
        Dim FenetreOpenGL As RenduGameWindow = New RenduGameWindow()
        Show()
    End Sub
    'déclaration d'une sub normale sans la clause Handles.
    Private Sub Button2_Click(sender As Object, e As EventArgs) 'Handles Button2.Click
        Hide()
        Using F As RenduControlGL = New RenduControlGL
            F.ShowDialog()
        End Using
        Show()
    End Sub
    'déclaration d'une sub normale sans la clause Handles.
    Private Sub Button3_Click(sender As Object, e As EventArgs) 'Handles Button3.Click
        Hide()
        Using F As RenduControlGL_2 = New RenduControlGL_2
            F.ShowDialog()
        End Using
        Show()
    End Sub
    'déclaration d'une sub normale sans la clause Handles.
    Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs) 'Handles MyBase.KeyDown
        If e.KeyCode = Keys.Escape Then Close()
    End Sub
End Class
