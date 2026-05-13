using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Models.Modals
{
    public class SingleInputModal : IModal
    {
        public string Title => "Input";

        [ModalTextInput("input1")]
        public string Input1 { get; set; } = string.Empty;
    }
}
