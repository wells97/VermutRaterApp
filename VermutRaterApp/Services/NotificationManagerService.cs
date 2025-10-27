using Plugin.LocalNotification;
using System;

namespace VermutRaterApp.Services
{


    public class NotificationManagerService : INotificationManagerService
    {
        public async Task ShowVermutReminder()
        {
            var notification = new NotificationRequest
            {
                NotificationId = 2001,
                Title = "Hora del vermut!",
                Description = "Avui també pots tastar un vermut nou 🍷"
            };
            var result = await LocalNotificationCenter.Current.RequestNotificationPermission();
            if (!result)
            {
                return;
            }
            LocalNotificationCenter.Current.Show(notification); // Assegura't que sigui LocalNotificationCenter
        }
    }
}
