namespace GW2AddonManager.Core.Services
{
    public interface IDialogService
    {
        public MessageBoxResult ShowMessageBox(string content, string title = "Message Box", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, int delay = 0);
    }

    public enum MessageBoxResult
    {
        /// <summary>
        /// The message box returns no result.
        /// </summary>
        None,
        /// <summary>
        /// The result value of the message box is OK.
        /// </summary>
        OK,
        /// <summary>
        /// The result value of the message box is Cancel.
        /// </summary>
        Cancel,
        /// <summary>
        /// The result value of the message box is Yes.
        /// </summary>
        Yes,
        /// <summary>
        /// The result value of the message box is No.
        /// </summary>
        No
    }

    [Flags]
    public enum MessageBoxButton
    {   
        OK = 0b00000001,     
        Cancel = 0b00000010,     
        Yes = 0b00000100,  
        No = 0b00001000
    }

    public enum MessageBoxImage
    {
        /// <summary>
        /// The message box contains no symbols.
        /// </summary>        
        None,
        /// <summary>
        ///  The message box contains a symbol consisting of white X in a circle with a red
        ///  background.
        /// </summary>
        Error,
        /// <summary>
        /// The message box contains a symbol consisting of a white X in a circle with a
        /// red background.
        /// </summary>
        Hand,
        /// <summary>
        /// The message box contains a symbol consisting of white X in a circle with a red
        /// background.
        /// </summary>
        Stop,
        /// <summary>
        /// The message box contains a symbol consisting of an exclamation point in a triangle
        /// with a yellow background.
        /// </summary>
        Exclamation,
        /// <summary>
        /// The message box contains a symbol consisting of an exclamation point in a triangle
        /// with a yellow background.
        /// </summary>
        Warning,
        /// <summary>
        /// The message box contains a symbol consisting of a lowercase letter i in a circle.
        Asterisk,
        /// <summary>
        /// The message box contains a symbol consisting of a lowercase letter i in a circle.
        /// </summary>
        Information
    }
}
