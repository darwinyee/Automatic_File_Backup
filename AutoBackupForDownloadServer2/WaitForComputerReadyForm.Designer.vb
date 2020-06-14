<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class WaitForComputerReadyForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.btnStopWaitingReady = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'btnStopWaitingReady
        '
        Me.btnStopWaitingReady.Location = New System.Drawing.Point(33, 25)
        Me.btnStopWaitingReady.Name = "btnStopWaitingReady"
        Me.btnStopWaitingReady.Size = New System.Drawing.Size(230, 55)
        Me.btnStopWaitingReady.TabIndex = 0
        Me.btnStopWaitingReady.Text = "Don't Wait"
        Me.btnStopWaitingReady.UseVisualStyleBackColor = True
        '
        'WaitForComputerReadyForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(297, 103)
        Me.ControlBox = False
        Me.Controls.Add(Me.btnStopWaitingReady)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Name = "WaitForComputerReadyForm"
        Me.Text = "Auto File Transferer Waiting Computer to be ready...."
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents btnStopWaitingReady As System.Windows.Forms.Button
End Class
