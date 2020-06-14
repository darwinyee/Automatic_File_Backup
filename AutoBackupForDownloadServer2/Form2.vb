Public Class Form2

    Private Sub btnOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOK.Click
        Try
            Dim temp As Int64 = CLng(txtBufferSize.Text)
            Form1.bufferSize = temp * 1073741824
            Form1.PopulateListView({"Destination Buffer Size Change ", Math.Round(Form1.bufferSize / 1073741824, 2).ToString & "GB"})
            Me.Close()
        Catch ex As Exception
            MessageBox.Show("Value must be numeric!")
            txtBufferSize.Focus()
        End Try
    End Sub
End Class