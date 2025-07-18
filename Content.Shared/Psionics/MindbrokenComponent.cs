// SPDX-FileCopyrightText: 2023 PHCodes <47927305+PHCodes@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 ShatteredSwords <135023515+ShatteredSwords@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class MindbrokenComponent : Component
    {
        /// <summary>
        ///     The text that will appear when someone with the Mindbroken component is examined at close range
        /// </summary>
        [DataField]
        public string MindbrokenExaminationText = "examine-mindbroken-message";
    }
}
