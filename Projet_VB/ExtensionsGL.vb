Friend Class ExtensionsGL
    Private OpenGLExtensions As HashSet(Of String)
    Friend Sub New()
        Dim count = GL.GetInteger(GetPName.NumExtensions)
        OpenGLExtensions = New HashSet(Of String)()
        For i = 0 To count - 1
            Dim extension = GL.GetString(StringNameIndexed.Extensions, i)
            OpenGLExtensions.Add(extension)
        Next i
    End Sub
    Friend ReadOnly Property IsSupported(NomExtensions As String) As Boolean
        Get
            Return OpenGLExtensions.Contains(NomExtensions)
        End Get
    End Property
End Class
