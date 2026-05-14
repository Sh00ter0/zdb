using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Mediators
{
    public class DiscordStateMediator
    {
        public DiscordHealthState HealthState = DiscordHealthState.Offline;

        public event Action<DiscordHealthState> StateChanged;

        public void ChangeState(DiscordHealthState newState)
        {
            if (HealthState != newState)
            {
                StateChanged?.Invoke(newState);
            }
            HealthState = newState;
        }
    }
}
