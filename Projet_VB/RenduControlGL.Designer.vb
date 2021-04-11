Partial Class RenduControlGL
    Inherits System.Windows.Forms.Form
    Friend Sub New()
        InitializeComponent()
        AjouterEvenementFormualaire()
    End Sub
    Private Sub AjouterEvenementFormualaire()
        'Evenements des contrôles du formulaire autres que ceux de RenduGL

        'Evenements du formulaire
        AddHandler Me.Resize, New EventHandler(AddressOf FormControlOpenTK_Resize)
        AddHandler Me.Load, New EventHandler(AddressOf FormControlOpenTK_Load)
        AddHandler Me.FormClosing, New FormClosingEventHandler(AddressOf FormControlOpenTK_Closing)
        AddHandler Me.FormClosed, New FormClosedEventHandler(AddressOf FormControlOpenTK_Closed)
    End Sub

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Requise par le Concepteur Windows Form
    Private components As System.ComponentModel.IContainer

    'REMARQUE : la procédure suivante est requise par le Concepteur Windows Form
    'Elle peut être modifiée à l'aide du Concepteur Windows Form.  
    'Ne la modifiez pas à l'aide de l'éditeur de code.
    '<System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.SuspendLayout()
        '
        'RenduControlGL
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.ClientSize = New System.Drawing.Size(590, 450)
        Me.Name = "RenduControlGL"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "ControlOpenTK"
        Me.ResumeLayout(False)

    End Sub
End Class
