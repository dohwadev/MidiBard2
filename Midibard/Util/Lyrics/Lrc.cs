﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using Dalamud.Logging;
using MidiBard.Control.MidiControl;
using MidiBard.Managers.Agents;
using MidiBard.Managers.Ipc;
using Dalamud;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard.Util.Lyrics
{
    public class Lrc
    {
        public static Lrc _lrc;
        /// <summary>
        /// 歌曲
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 艺术家
        /// </summary>
        public string Artist { get; set; }
        /// <summary>
        /// 专辑
        /// </summary>
        public string Album { get; set; }
        /// <summary>
        /// 歌词作者
        /// </summary>
        public string LrcBy { get; set; }
        /// <summary>
        /// 偏移量
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// 歌词
        /// </summary>
        public Dictionary<double, string> LrcWord = new Dictionary<double, string>();

        /// <summary>
        /// Get lyric info
        /// </summary>
        /// <param name="LrcPath">path of lrc file</param>
        /// <returns>returns lyrics info</returns>
        public static void InitLrc(string midiFilePath)
        {
            _lrc = new Lrc();
            bool loadSuccessfull = true;
            Dictionary<double, string> dicword = new Dictionary<double, string>();

            string[] pathArray = midiFilePath.Split("\\");
            string LrcPath = "";
            string fileName = Path.GetFileNameWithoutExtension(midiFilePath) + ".lrc";
            for (int i = 0; i < pathArray.Length - 1; i++)
            {
                LrcPath += pathArray[i];
                LrcPath += "\\";
            }

            LrcPath += fileName;

            try
            {
                using (FileStream fs = new FileStream(LrcPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    string line;
                    using (StreamReader sr = new StreamReader(fs, Encoding.Default))
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.StartsWith("[ti:"))
                            {
                                _lrc.Title = SplitInfo(line);
                            }
                            else if (line.StartsWith("[ar:"))
                            {
                                _lrc.Artist = SplitInfo(line);
                            }
                            else if (line.StartsWith("[al:"))
                            {
                                _lrc.Album = SplitInfo(line);
                            }
                            else if (line.StartsWith("[by:"))
                            {
                                _lrc.LrcBy = SplitInfo(line);
                            }
                            else if (line.StartsWith("[offset:"))
                            {
                                long offset;
                                long.TryParse(SplitInfo(line), out offset);
                                _lrc.Offset = offset;
                            }
                            else
                            {
                                try
                                {
                                    Regex regexword = new Regex(@".*\](.*)");
                                    Match mcw = regexword.Match(line);
                                    string word = mcw.Groups[1].Value;
                                    Regex regextime = new Regex(@"\[([0-9.:]*)\]", RegexOptions.Compiled);
                                    MatchCollection mct = regextime.Matches(line);
                                    foreach (Match item in mct)
                                    {
                                        double time = TimeSpan.Parse("00:" + item.Groups[1].Value).TotalSeconds;
                                        dicword.Add(time, word);
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
                _lrc.LrcWord = dicword.OrderBy(t => t.Key).ToDictionary(t => t.Key, p => p.Value);
            }
            catch (Exception ex)
            {
                loadSuccessfull = false;
                //PluginLog.LogError(ex.ToString());
            }

            if (loadSuccessfull)
            {
                PluginLog.Log($"Load LRC: {LrcPath}");
                LrcTimeStamps = _lrc.LrcWord.Keys.ToList();
                api.ChatGui.Print(string.Format("[MidiBard 2] Lyrics Loaded: {0}", LrcPath));
            }
        }

        /// <param name="line"></param>
        /// <returns>returns the basic info</returns>
        static string SplitInfo(string line)
        {
            return line.Substring(line.IndexOf(":") + 1).TrimEnd(']').TrimStart();
        }

        public static bool HasLyric()
        {
            return _lrc == null ? false : _lrc.LrcWord.Count > 0;
        }

        static string ProcessLine(string line, ref string characterName)
        {
            string lyric = "";

            var idx = line.IndexOf(":");
            if (idx < 0)
            {
                return line;
            } else
            {
                characterName = line.Substring(0, idx);
                lyric = line.Substring(idx+1, line.Length-1-idx);
            }

            return lyric;
        }

        public static int LrcIdx = -1;
        internal static int LRCDeltaTime = 50;
        static bool SongTitlePosted = false;

        public static List<double> LrcTimeStamps = new List<double>();

        public static bool LrcLoaded()
        {
            return api.PartyList.Length > 1 && LrcTimeStamps.Count > 0;
        }

        public static void Play()
        {
            LRCDeltaTime = 100; // Assume usual delay between sending and other clients receiving the message would be ~100ms

            if (HasLyric())
            {
                if (api.PartyList.Length <= 1)
                {
                    api.ChatGui.Print(string.Format("[MidiBard 2] Not in a party, Lyrics will not be posted."));
                }
            }

            try
            {
                LrcTimeStamps = _lrc.LrcWord.Keys.ToList();
                if (MidiPlayerControl._stat != MidiPlayerControl.e_stat.Paused)
                {
                    LrcIdx = -1;
                }
            }
            catch (Exception e)
            {
                PluginLog.LogError(e.Message);
            }
        }

        public static void Stop()
        {
            LrcIdx = -1;
            SongTitlePosted = false;
        }

        internal static void ChangeLRCDeltaTime(int delta)
        {
            if (MidiBard.CurrentPlayback == null || !MidiBard.CurrentPlayback.IsRunning)
            {
                LRCDeltaTime = 100;
                return;
            }

            LRCDeltaTime += delta;
        }

        public static void EnsembleStart()
        {
            _lrc.Offset += (long)(4.045 * 1000); // a hack way to get ensemble delay, see MidiFilePlot.cs:90 
            //PluginLog.LogVerbose("LRC Offset: " + _lrc.Offset);
        }

        public static void Tick(Dalamud.Game.Framework framework)
        {
            try
            {
                if (!MidiBard.config.playLyrics || MidiPlayerControl._stat != MidiPlayerControl.e_stat.Playing)
                {
                    return;
                }

                if (MidiPlayerControl._stat == MidiPlayerControl.e_stat.Playing && HasLyric()
                    && !SongTitlePosted && api.PartyList.IsPartyLeader())
                {
                    string msg = "";
                    msg = $"♪ {_lrc.Title} ♪ ";
                    msg += _lrc.Artist != null && _lrc.Artist != "" ? $"Artist: {_lrc.Artist} ♪ " : "";
                    msg += _lrc.Album != null && _lrc.Album != "" ? $"Album: {_lrc.Album} ♪ " : "";
                    msg += _lrc.LrcBy != null && _lrc.LrcBy != "" ? $"Lyric By: {_lrc.LrcBy} ♪ " : "";

                    if (!MidiBard.AgentMetronome.EnsembleModeRunning)
                    {
                        msg = "/p " + msg;
                    }

                    Chat.SendMessage(msg);
                    SongTitlePosted = true;
                    return;
                }

                if (LrcTimeStamps.Count > 0 && LrcIdx < LrcTimeStamps.Count)
                {
                    int idx = FindLrcIdx(LrcTimeStamps);
                    if (idx < 0 || idx == LrcIdx)
                    {
                        return;
                    }
                    else
                    {
                        bool postLyrics = false;
                        string characterName = "";
                        string lyric = ProcessLine(_lrc.LrcWord[LrcTimeStamps[idx]], ref characterName);
                        PluginLog.LogDebug($"{characterName}, {lyric}");
                        if (characterName == "" && api.PartyList.IsPartyLeader())
                        {
                            postLyrics = true;
                        } else if (characterName != "")
                        {
                            if ((bool)api.ClientState.LocalPlayer?.Name.TextValue.ToLower().Contains(characterName.ToLower()))
                            {
                                postLyrics = true;
                            }
                        }
                        PluginLog.LogVerbose($@"Post Lyrics:{postLyrics}");

                        if (postLyrics)
                        {
                            string msg = "";
                            
                            PluginLog.LogVerbose($"{lyric}");
                            if (MidiBard.AgentMetronome.EnsembleModeRunning)
                            {
                                PluginLog.LogVerbose("MidiBard.AgentMetronome.EnsembleModeRunning");
                                msg = $"/s ♪ {lyric} ♪";
                            }
                            else
                            {
                                msg = $"/p ♪ {lyric} ♪";
                            }
                            
                            Chat.SendMessage(msg);
                        }
                        LrcIdx = idx;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.LogError($"exception: {ex}");
            }
        }

        static int FindLrcIdx(List<double> TimeStamps)
        {
            if (TimeStamps.Count == 0)
                return -1;

            int idx = -1;
            double timeSpan = MidiBard.CurrentPlayback.GetCurrentTime<MetricTimeSpan>().TotalSeconds - _lrc.Offset / 1000.0f + LRCDeltaTime / 1000.0f;
            if (timeSpan < 0)
            {
                return -1;
            }

            foreach (double TimeStamp in TimeStamps)
            {
                if (timeSpan > TimeStamp)
                {
                    idx++;
                }
                else
                {
                    break;
                }
            }

            return idx >= TimeStamps.Count ? -1 : idx;
        }
    }
}
