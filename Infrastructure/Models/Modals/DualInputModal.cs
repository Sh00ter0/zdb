using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Models.Modals
{
    public class DualInputModal : IModal
    {
        public string Title => "Input";

        [ModalTextInput("input1")]
        public string Input1 { get; set; } = string.Empty;

        [ModalTextInput("input2")]
        public string Input2 { get; set; } = string.Empty;
    }
}
