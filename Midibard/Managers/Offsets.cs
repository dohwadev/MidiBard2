// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

using System;
using System.Diagnostics.CodeAnalysis;

namespace MidiBard.Managers;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public static class Offsets
{
    [StaticAddress("48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 4B 40")]
    public static IntPtr AgentMetronome { get; private set; }

    [StaticAddress("48 8D 05 ?? ?? ?? ?? 48 8B F9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 41 28 48 8B 49 48")]
    public static IntPtr AgentPerformance { get; private set; }

    [StaticAddress("48 8D 05 ?? ?? ?? ?? C7 83 E0 00 00 00 ?? ?? ?? ??")]
    public static IntPtr AgentConfigSystem { get; private set; }

    [StaticAddress("48 8B 15 ?? ?? ?? ?? F6 C2 ??")]
    public static IntPtr PerformanceStructPtr { get; private set; }

    [Function("48 89 6C 24 10 48 89 74 24 18 57 48 83 EC ?? 48 83 3D ?? ?? ?? ?? ?? 41 8B E8")]
    public static IntPtr DoPerformAction { get; private set; }

    [Offset("40 88 ?? ?? 66 89 ?? ?? 40 84", +3)]
    public static byte InstrumentOffset { get; private set; }

    //[Function("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 FA 48 8B D9 84 D2 ")]
    //public static IntPtr UpdateMetronome { get; private set; }

    [Function("48 8B C4 56 48 81 EC ?? ?? ?? ?? 48 89 58 10 ")]
    public static IntPtr ApplyGraphicConfigsFunc { get; private set; }

    [Function("40 53 48 83 EC 20 48 8B D9 48 83 C1 78 E8 ? ? ? ? 48 8D 8B ? ? ? ? E8 ? ? ? ? 48 8D 53 20 ")]
    public static IntPtr NetworkEnsembleStart { get; private set; }
#if DEBUG

    [Function("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 ")]
    public static IntPtr SoloReceivedHandler { get; private set; }

    [Function("4C 8B C2 8B D1 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 40 53 48 83 EC 20 48 8B D9 ")]
    public static IntPtr EnsembleReceivedHandler { get; private set; }

    [Function("4C 8B DC 49 89 6B 20 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 71 09 ")]
    public static IntPtr SoloSendHandler { get; private set; }

    [Function("40 55 57 41 56 48 8D AC 24 ?? ?? ?? ?? B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 0F B6 79 09 ")]
    public static IntPtr EnsembleSendHandler { get; private set; }

    [Function("E8 ?? ?? ?? ?? 48 8B D7 48 8D 4D D8 44 8B E0")]
    public static IntPtr ChangeOctave { get; private set; }

    [Function(" E8 ?? ?? ?? ?? 88 43 08 48 8B 74 24 ?? ")]
    public static IntPtr ChangeKeyboardLayout { get; private set; }

    [Function("89 54 24 10 53 55 57 41 54 41 55 41 56 48 83 EC 48 8B C2 45 8B E0 44 8B D2 45 32 F6 44 8B C2 45 32 ED")]
    public static IntPtr SetOption { get; private set; }

    [Function("E8 ?? ?? ?? ?? 48 03 87 ?? ?? ?? ?? ")]
    public static IntPtr GetErozeaTime { get; private set; }
#endif

}