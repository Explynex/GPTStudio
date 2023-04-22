using GPTStudio.MVVM.View.Controls;
using GPTStudio.MVVM.ViewModels;
using System;
using System.Windows;
using System.Windows.Media;

namespace GPTStudio.Utils
{
    internal static class Presentation
    {
        private static ChoiceView choicePopup = null;
        public static Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null)
            {
                return null;
            }
            if (element.GetType() == type)
            {
                return element;
            }
            Visual foundElement = null;
            if (element is FrameworkElement)
            {
                (element as FrameworkElement).ApplyTemplate();
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                {
                    break;
                }
            }
            return foundElement;
        }

        public static void OpenChoicePopup(string title,string text,Action successChoiceAction = null)
        {
            (App.Current.MainWindow.DataContext as MainWindowViewModel).PopupContent = choicePopup ??= new();

            choicePopup.SuccessAction    = successChoiceAction;
            choicePopup.Title.Text       = title;
            choicePopup.ContentText.Text = text;
            (App.Current.MainWindow.DataContext as MainWindowViewModel).IsPopupActive = true;
        }
    }
}
