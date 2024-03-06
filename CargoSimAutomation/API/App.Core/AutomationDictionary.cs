using App.Core.Services;
using App.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Core
{
    public static class AutomationDictionary
    {
        public static Dictionary<string, Automation> UserAutomation = new Dictionary<string, Automation>();

        public static void AddUserAutomation(string username, Automation automationService)
        {
            UserAutomation[username] = automationService;
        }

        public static Automation GetUserAutomation(string username)
        {
            if (UserAutomation.ContainsKey(username))
            {
                return UserAutomation[username];
            }
            return default;
        }
    }
}
