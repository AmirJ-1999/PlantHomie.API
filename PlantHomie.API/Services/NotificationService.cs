using PlantHomie.API.Data;
using PlantHomie.API.Models;

namespace PlantHomie.API.Services
{
    public class NotificationService
    {
        private readonly PlantHomieContext _context;

        public NotificationService(PlantHomieContext context)
        {
            _context = context;
        }

        public async Task CheckAndSendNotificationAsync(Plant plant, User user, double temp, double soil, double humidity)
        {
            //Her er der lavet nogle grænseværdier som kun bruges til at vurdere om målingerne fra raspberry pi er "gode" eller "kritiske", fx skla temepratur være mellem 10 og 30
            bool outsideNormal =
                temp < 10 || temp > 30 ||
                soil < 20 || soil > 80 ||
                humidity < 30 || humidity > 70;

            bool critical =
                temp < 5 || temp > 35 ||
                soil < 10 || soil > 90 ||
                humidity < 20 || humidity > 80;

            // Since AutoMode is [NotMapped], we'll only send notifications for critical conditions
            if (critical)
            {
                var notification = new Notification
                {
                    User_ID = user.User_ID,
                    Plant_ID = plant.Plant_ID,
                    Dato_Tid = DateTime.UtcNow,
                    Plant_Type = plant.Plant_type
                };

                //Opretter og gemmer en Notification i databasen
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
        }
    }
}