Public Class WaitForComputerReadyForm

    Private Sub btnStopWaitingReady_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnStopWaitingReady.Click
        Form1.TimeCounter.Stop()
        Form1.WaitForComputerReadyTimer.Interval = 100
        Me.Close()
    End Sub
End Class