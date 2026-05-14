using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Mediators
{
    public class DiscordStateMediator
    {
        public event Action<DiscordHealthState> StateChanged;

        public void ChangeState(DiscordHealthState newState)
        {
            StateChanged?.Invoke(newState);
        }
    }
}
