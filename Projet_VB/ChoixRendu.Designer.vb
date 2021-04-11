Partial Class ChoixRendu
    Inherits Form
    'Ne pas oublier le constructeur
    Friend Sub New()
        InitializeComponent()
        EvenementsMenu()
    End Sub
    'Form remplace la méthode Dispose pour nettoyer la liste des composants.
    '<System.Diagnostics.DebuggerNonUserCode()>
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
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'Button1
        '
        Me.Button1.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.Location = New System.Drawing.Point(12, 12)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(164, 33)
        Me.Button1.TabIndex = 0
        Me.Button1.Text = "OpenTK GameWindow"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button2.Location = New System.Drawing.Point(12, 51)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(164, 29)
        Me.Button2.TabIndex = 1
        Me.Button2.Text = "OpenTK ControlGL_1"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button3.Location = New System.Drawing.Point(12, 86)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(164, 29)
        Me.Button3.TabIndex = 2
        Me.Button3.Text = "OpenTK ControlGL_2"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'ChoixRendu
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(192, 128)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.KeyPreview = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "ChoixRendu"
        Me.Text = "OpenTK VB"
        Me.ResumeLayout(False)

    End Sub

    'Ajout par rapport au designer VB. Partie cachée par le compilateur avec le déclarateur WithEvents
    Private Sub EvenementsMenu()
        'Evenements des contrôles du formulaire
        AddHandler Me.Button1.Click, New EventHandler(AddressOf Button1_Click)
        AddHandler Me.Button2.Click, New EventHandler(AddressOf Button2_Click)
        AddHandler Me.Button3.Click, New EventHandler(AddressOf Button3_Click)
        'Evenements du formulaire
        AddHandler Me.KeyDown, New KeyEventHandler(AddressOf Form1_KeyDown)
        AddHandler Me.Load, New EventHandler(AddressOf Form1_Load)
    End Sub

    'Suppression par rapport au désigner VB. Déclaration d'une variable avec le déclarateur WithEvents
    'Friend WithEvents Button1 As Button
    'Friend WithEvents Button2 As Button
    'Friend WithEvents Button3 As Button
    'Ajout par rapport au designer VB. déclaration normale d'une variable au lieu du declarateur WithEvents
    Friend Button1 As Button
    Friend Button2 As Button
    Friend Button3 As Button
End Class
