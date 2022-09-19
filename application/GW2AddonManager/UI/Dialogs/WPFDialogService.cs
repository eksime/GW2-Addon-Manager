namespace GW2AddonManager.UI.Dialogs
{
    using System;
    using GW2AddonManager.Core.Services;

    public class WPFDialogService : IDialogService
    {
        public MessageBoxResult ShowMessageBox(
        string content,
        string title = "Message Box",
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage image = MessageBoxImage.None,
        int delay = 0)
        {

            System.Windows.MessageBoxButton winButtons = buttons switch
            {
                MessageBoxButton.OK => System.Windows.MessageBoxButton.OK,
                (MessageBoxButton.OK | MessageBoxButton.Cancel) => System.Windows.MessageBoxButton.OKCancel,
                (MessageBoxButton.Yes | MessageBoxButton.No | MessageBoxButton.Cancel) => System.Windows.MessageBoxButton.YesNoCancel,
                (MessageBoxButton.Yes | MessageBoxButton.No) => System.Windows.MessageBoxButton.YesNo,
                _ => throw new ArgumentException("Button combination not supported in WPF.", nameof(buttons))
            };

            System.Windows.MessageBoxImage winImg = image switch
            {
                MessageBoxImage.None => System.Windows.MessageBoxImage.None,
                MessageBoxImage.Error => System.Windows.MessageBoxImage.Error,
                MessageBoxImage.Hand => System.Windows.MessageBoxImage.Hand,
                MessageBoxImage.Stop => System.Windows.MessageBoxImage.Stop,
                MessageBoxImage.Exclamation => System.Windows.MessageBoxImage.Exclamation,
                MessageBoxImage.Warning => System.Windows.MessageBoxImage.Warning,
                MessageBoxImage.Asterisk => System.Windows.MessageBoxImage.Asterisk,
                MessageBoxImage.Information => System.Windows.MessageBoxImage.Information,
                _ => throw new ArgumentException("Image not supported in WPF.", nameof(image))
            };

            Popup p = new Popup(System.Windows.Application.Current?.MainWindow, content, title, winButtons, winImg, delay);
            _ = p.ShowDialog();

            MessageBoxResult dialogResult = p.Result switch
            {
                System.Windows.MessageBoxResult.None => MessageBoxResult.None,
                System.Windows.MessageBoxResult.OK => MessageBoxResult.OK,
                System.Windows.MessageBoxResult.Cancel => MessageBoxResult.Cancel,
                System.Windows.MessageBoxResult.Yes => MessageBoxResult.Yes,
                System.Windows.MessageBoxResult.No => MessageBoxResult.No,
                _ => throw new ArgumentException("Message box result unsupported.", nameof(dialogResult))
            };

            return dialogResult;
        }
    }
}
