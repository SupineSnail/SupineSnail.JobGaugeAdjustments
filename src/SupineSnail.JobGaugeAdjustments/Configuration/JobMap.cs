using System.Collections.ObjectModel;

namespace SupineSnail.JobGaugeAdjustments.Configuration;

internal static class JobMap
{
    private static ReadOnlyDictionary<uint, JobGaugeMap> _map;

    public static ReadOnlyDictionary<uint, JobGaugeMap> Map => _map ?? CreateJobMap();
    
    
    public static JobGaugeMap GetJobMap(uint? jobId) {
        if (!jobId.HasValue)
            return null;
            
        Map.TryGetValue(jobId.Value, out var uiMap);
        return uiMap;
    }

    private static ReadOnlyDictionary<uint, JobGaugeMap> CreateJobMap()
    {
        var map = new Dictionary<uint, JobGaugeMap>
        {
            {
                19,
                new JobGaugeMap(19, "Paladin")
                {
                    Addons = new()
                    {
                        {
                            "JobHudPLD0",
                            new[]
                            {
                                new AddonComponentPart("Oath", "Oath Gauge", 18),
                                new AddonComponentPart("OathText", "Oath Gauge Value", false, true, 17),
                                new AddonComponentPart("IronWill", "Iron Will Icon", 15)
                            }
                        }
                    }
                }
            },
            {
                20,
                new JobGaugeMap(20, "Monk")
                {
                    Addons = new()
                    {
                        {
                            "JobHudMNK1",
                            new[]
                            {
                                new AddonComponentPart("Chakra1", "Chakra 1", 18),
                                new AddonComponentPart("Chakra2", "Chakra 2", 19),
                                new AddonComponentPart("Chakra3", "Chakra 3", 20),
                                new AddonComponentPart("Chakra4", "Chakra 4", 21),
                                new AddonComponentPart("Chakra5", "Chakra 5", 22)
                            }
                        },
                        {
                            "JobHudMNK0",
                            new[]
                            {
                                new AddonComponentPart("Master", "Master Timeout", false, true, 39),
                                new AddonComponentPart("MChakra", "Master Chakras", 33),
                                new AddonComponentPart("Nadi", "Nadi", 25),
                            }
                        }
                    }
                }
            },
            {
                21,
                new JobGaugeMap(21, "Warrior")
                {
                    Addons = new()
                    {
                        {
                            "JobHudWAR0",
                            new[]
                            {
                                new AddonComponentPart("Defiance", "Defiance Icon", 14),
                                new AddonComponentPart("Beast", "Beast Gauge", 17),
                                new AddonComponentPart("BeastValue", "Beast Gauge Value", false, true, 16),
                            }
                        }
                    }
                }
            },
            {
                22,
                new JobGaugeMap(22, "Dragoon")
                {
                    Addons = new()
                    {
                        {
                            "JobHudDRG0",
                            new[]
                            {
                                new AddonComponentPart("LotD", "LotD Bar", 43),
                                new AddonComponentPart("LotDValue", "LotD Bar Value", false, true, 42),
                                new AddonComponentPart("Gaze1", "Gaze 1", 36),
                                new AddonComponentPart("Gaze2", "Gaze 2", 37),
                                new AddonComponentPart("Firstmind1", "Firstmind 1", 39),
                                new AddonComponentPart("Firstmind2", "Firstmind 2", 40),
                            }
                        }
                    }
                }
            },
            {
                23,
                new JobGaugeMap(23, "Bard")
                {
                    Addons = new()
                    {
                        {
                            "JobHudBRD0",
                            new[]
                            {
                                new AddonComponentPart("SongGauge", "Song Gauge", 99),
                                new AddonComponentPart("SongGaugeValue", "Song Gauge Value", false, true, 97),
                                new AddonComponentPart("SongName", "Song Name", 76),
                                new AddonComponentPart("Repertoire1", "Repertoire 1", 90, 94),
                                new AddonComponentPart("Repertoire2", "Repertoire 2", 91, 95),
                                new AddonComponentPart("Repertoire3", "Repertoire 3", 92, 96),
                                new AddonComponentPart("Repertoire4", "Repertoire 4", 93),
                                new AddonComponentPart("SoulVoiceBar", "Soul Voice Bar", 87),
                                new AddonComponentPart("SoulVoiceText", "Soul Voice Text", false, true, 86),
                                new AddonComponentPart("MageCoda", "Mage's Coda", 79, 82),
                                new AddonComponentPart("ArmyCoda", "Army's Coda", 80, 84),
                                new AddonComponentPart("WandererCoda", "Wanderer's Coda", 81, 83)
                            }
                        }
                    }
                }
            },
            {
                24,
                new JobGaugeMap(24, "White Mage")
                {
                    Addons = new()
                    {
                        {
                            "JobHudWHM0",
                            new[]
                            {
                                new AddonComponentPart("LilyBar", "Lily Bar", 38),
                                new AddonComponentPart("Lily1", "Lily 1", 30),
                                new AddonComponentPart("Lily2", "Lily 2", 31),
                                new AddonComponentPart("Lily3", "Lily 3", 32),
                                new AddonComponentPart("BloodLily1", "Blood Lily 1", 34),
                                new AddonComponentPart("BloodLily2", "Blood Lily 2", 35),
                                new AddonComponentPart("BloodLily3", "Blood Lily 3", 36),
                            }
                        }
                    }
                }
            },
            {
                25,
                new JobGaugeMap(25, "Black Mage")
                {
                    Addons = new()
                    {
                        {
                            "JobHudBLM0",
                            new[]
                            {
                                new AddonComponentPart("CountdownText", "Countdown Text", false, true, 36),
                                new AddonComponentPart("Ele1", "Ice/Fire 1", 42),
                                new AddonComponentPart("Ele2", "Ice/Fire 2", 43),
                                new AddonComponentPart("Ele3", "Ice/Fire 3", 44),
                                new AddonComponentPart("Heart1", "Heart 1", 38),
                                new AddonComponentPart("Heart2", "Heart 2", 39),
                                new AddonComponentPart("Heart3", "Heart 3", 40),
                                new AddonComponentPart("PolygotGauge", "Polygot Gauge", 48),
                                new AddonComponentPart("Polygot1", "Polygot 1", 46),
                                new AddonComponentPart("Polygot2", "Polygot 2", 47),
                                new AddonComponentPart("Endochan", "Endochan", 33),
                                new AddonComponentPart("ParadoxGauge", "Paradox Gauge", 34)
                            }
                        }
                    }
                }
            },
            {
                27,
                new JobGaugeMap(27, "Summoner")
                {
                    Addons = new()
                    {
                        {
                            "JobHudSMN1",
                            new[]
                            {
                                new AddonComponentPart("TranceGauge", "Trance Gauge", 56),
                                new AddonComponentPart("TranceCountdown", "Trance Countdown", false, true, 55),
                                new AddonComponentPart("RubyArcanum", "Ruby Arcanum", 51),
                                new AddonComponentPart("TopazArcanum", "Topaz Arcanum", 52),
                                new AddonComponentPart("EmeraldArcanum", "Emerald Arcanum", 53),
                                new AddonComponentPart("PetCountdown", "Pet Countdown", false, true, 50),
                                new AddonComponentPart("PetIcon", "Pet Icon", 49),
                                new AddonComponentPart("BahamutPhoenix", "Bahamut/Phoenix", 47),
                            }
                        },
                        {
                            "JobHudSMN0",
                            new[]
                            {
                                new AddonComponentPart("Aetherflow1", "Aetherflow 1", 12),
                                new AddonComponentPart("Aetherflow2", "Aetherflow 2", 13),
                            }
                        }
                    }
                }
            },
            {
                28,
                new JobGaugeMap(28, "Scholar")
                {
                    Addons = new()
                    {
                        {
                            "JobHudSCH0",
                            new[]
                            {
                                new AddonComponentPart("FaireGauge", "Faire Gauge", 32),
                                new AddonComponentPart("FaireGaugeValue", "Faire Gauge Value", false, true, 31),
                                new AddonComponentPart("SeraphIcon", "Seraph Icon", 29),
                                new AddonComponentPart("SeraphCountdown", "Seraph Countdown", false, true, 30),
                            }
                        },
                        {
                            "JobHudACN0",
                            new[]
                            {
                                new AddonComponentPart("Aetherflow1", "Aetherflow 1", 8),
                                new AddonComponentPart("Aetherflow2", "Aetherflow 2", 9),
                                new AddonComponentPart("Aetherflow3", "Aetherflow 3", 10),
                            }
                        }
                    }
                }
            },
            {
                30,
                new JobGaugeMap(30, "Ninja")
                {
                    Addons = new()
                    {
                        {
                            "JobHudNIN1",
                            new[]
                            {
                                new AddonComponentPart("HutonBar", "Huton Bar", 20),
                                new AddonComponentPart("HutonBarValue", "Huton Bar Value", false, true, 19),
                                new AddonComponentPart("HutonClockIcon", "Huton Clock Icon", 18)
                            }
                        },
                        {
                            "JobHudNIN0",
                            new[]
                            {
                                new AddonComponentPart("NinkiGauge", "Ninki Gauge", 19),
                                new AddonComponentPart("NinkiGaugeValue", "Ninki Gauge Value", false, true, 18)
                            }
                        }
                    }
                }
            },
            {
                31,
                new JobGaugeMap(31, "Machinist")
                {
                    Addons = new()
                    {
                        {
                            "JobHudMCH0",
                            new[]
                            {
                                new AddonComponentPart("HeatGauge", "Heat Gauge", 38),
                                new AddonComponentPart("HeatValue", "Heat Gauge Value", false, true, 37),
                                new AddonComponentPart("OverheatIcon", "Overheat Icon", 36),
                                new AddonComponentPart("OverheatCountdown", "Overheat Countdown", false, true, 35),
                                new AddonComponentPart("BatteryGauge", "Battery Gauge", 43),
                                new AddonComponentPart("BatteryValue", "Battery Value", false, true, 42),
                                new AddonComponentPart("QueenIcon", "Queen Icon", 41),
                                new AddonComponentPart("QueenCountdown", "QueenCountdown", 40),
                            }
                        }
                    }
                }
            },
            {
                32,
                new JobGaugeMap(32, "Dark Knight")
                {
                    Addons = new()
                    {
                        {
                            "JobHudDRK0",
                            new[]
                            {
                                new AddonComponentPart("GritIcon", "Grit Icon", 15),
                                new AddonComponentPart("BloodGauge", "Blood Gauge", 18),
                                new AddonComponentPart("BloodGaugeValue", "Blood Gauge Value", false, true, 17)
                            }
                        },
                        {
                            "JobHudDRK1",
                            new[]
                            {
                                new AddonComponentPart("DarksideGauge", "Darkside Gauge", 27),
                                new AddonComponentPart("DarksideGaugeValue", "Darkside Gauge Value", false, true, 26),
                                new AddonComponentPart("DarkArts", "Dark Arts", 24),
                                new AddonComponentPart("LivingShadow", "Living Shadow", 22),
                                new AddonComponentPart("LivingShadowValue", "Living Shadow Value", false, true, 23)
                            }
                        }
                    }
                }
            },
            {
                33,
                new JobGaugeMap(33, "Astrologian")
                {
                    Addons = new()
                    {
                        {
                            "JobHudAST0",
                            new[]
                            {
                                new AddonComponentPart("Background", "Background", 42, 43),
                                new AddonComponentPart("Arcanum", "Arcanum", 38),
                                new AddonComponentPart("DrawBackground", "Arcanum Background", 40),
                                new AddonComponentPart("AstrosignBkg", "Astrosign Background", 36),
                                new AddonComponentPart("Astrosigns", "Astrosigns", 33, 34, 35),
                                new AddonComponentPart("MinorArcanum", "Minor Arcanum", 39),
                                new AddonComponentPart("MinorBackground", "Minor Arcanum Background", 41),
                            }
                        }
                    }
                }
            },
            {
                34,
                new JobGaugeMap(34, "Samurai")
                {
                    Addons = new()
                    {
                        {
                            "JobHudSAM1",
                            new[]
                            {
                                new AddonComponentPart("Sen1", "Sen 1", 41),
                                new AddonComponentPart("Sen2", "Sen 2", 45),
                                new AddonComponentPart("Sen3", "Sen 3", 49),
                            }
                        },
                        {
                            "JobHudSAM0",
                            new[]
                            {
                                new AddonComponentPart("Kenki", "Kenki Gauge", 31),
                                new AddonComponentPart("KenkiValue", "Kenki Value", false, true, 30),
                                new AddonComponentPart("Meditation1", "Meditation 1", 26),
                                new AddonComponentPart("Meditation2", "Meditation 2", 27),
                                new AddonComponentPart("Meditation3", "Meditation 3", 28),
                            }
                        }
                    }
                }
            },
            {
                35,
                new JobGaugeMap(35, "Red Mage")
                {
                    Addons = new()
                    {
                        {
                            "JobHudRDM0",
                            new[]
                            {
                                new AddonComponentPart("WhiteManaBar", "White Mana Bar", 38),
                                new AddonComponentPart("WhiteManaValue", "White Mana Value", false, true, 25),
                                new AddonComponentPart("BlackManaBar", "Black Mana Bar", 39),
                                new AddonComponentPart("BlackManaValue", "Black Mana Value", false, true, 26),
                                new AddonComponentPart("StatusIndicator", "Status Indicator", 35),
                                new AddonComponentPart("ManaStack1", "Mana Stack 1", 28),
                                new AddonComponentPart("ManaStack2", "Mana Stack 2", 29),
                                new AddonComponentPart("ManaStack3", "Mana Stack 3", 30)
                            }
                        }
                    }
                }
            },
            {
                37,
                new JobGaugeMap(37, "Gunbreaker")
                {
                    Addons = new()
                    {
                        {
                            "JobHudGNB0",
                            new[]
                            {
                                new AddonComponentPart("RoyalGuard", "Royal Guard Icon", 24),
                                new AddonComponentPart("Cart1", "Cartridge 1", 20),
                                new AddonComponentPart("Cart2", "Cartridge 2", 22),
                                new AddonComponentPart("Cart3", "Cartridge 3", 23),
                            }
                        }
                    }
                }
            },
            {
                38,
                new JobGaugeMap(38, "Dancer", true)
                //     {
                //         "JobHudDNC0",
                //         new[] {
                //             new AddonComponentPart("Waiting","Waiting Icon", 13),
                //             new AddonComponentPart("Standard","Standard Step Background", 14),
                //             new AddonComponentPart("StandardGlow","Step Diamonds", 12),
                //             new AddonComponentPart("StandardIcon","Standard Step Icon", 8),
                //             new AddonComponentPart("StandardText","Standard Step Countdown", 10, 11),
                //             new AddonComponentPart("Step1","Step 1", 3),
                //             new AddonComponentPart("Step2","Step 2", 4),
                //             new AddonComponentPart("Step3","Step 3", 5),
                //             new AddonComponentPart("Step4","Step 4", 38),
                //             new AddonComponentPart("StepHighlight","Current Step Highlight", 4),
                //             
                //             new AddonComponentPart("TechnicalBkg","Technical Step Background", 15),
                //         }
                //     },
                //     {
                //         "JobHudDNC1",
                //         new[] {
                //             new AddonComponentPart("Feather1","Fourfold Feather 1", 4),
                //             new AddonComponentPart("Feather2","Fourfold Feather 2", 5),
                //             new AddonComponentPart("Feather3","Fourfold Feather 3", 6),
                //             new AddonComponentPart("Feather4","Fourfold Feather 4", 7),
                //             new AddonComponentPart("Espirt","Espirt Gauge", 10),
                //             new AddonComponentPart("Espirt Value","Espirt Value", 8),
                //         }
                //     }
            },
            {
                39,
                new JobGaugeMap(39, "Reaper")
                {
                    Addons = new()
                    {
                        {
                            "JobHudRRP1",
                            new[]
                            {
                                new AddonComponentPart("Shroud1", "Lemure Shroud 1", 21),
                                new AddonComponentPart("Shroud2", "Lemure Shroud 2", 20),
                                new AddonComponentPart("Shroud3", "Lemure Shroud 3", 19),
                                new AddonComponentPart("Shroud4", "Lemure Shroud 4", 18),
                                new AddonComponentPart("Shroud5", "Lemure Shroud 5", 17),
                                new AddonComponentPart("Enshroud", "Enshroud Icon", 15),
                                new AddonComponentPart("EnshroudIcon", "Enshroud Countdown", false, true, 16)
                            }
                        },
                        {
                            "JobHudRRP0",
                            new[]
                            {
                                new AddonComponentPart("ShroudGauge", "Shroud Gauge", 45),
                                new AddonComponentPart("ShroudValue", "Shroud Gauge Value", false, true, 44),
                                new AddonComponentPart("DeathGauge", "Death Gauge", 42),
                                new AddonComponentPart("DeathValue", "Death Gauge Value", false, true, 41)
                            }
                        }
                    }
                }
            },
            {
                40,
                new JobGaugeMap(40, "Sage")
                {
                    Addons = new()
                    {
                        {
                            "JobHudGFF1",
                            new[]
                            {
                                new AddonComponentPart("AddersgallGauge", "Addersgall Gauge", 34),
                                new AddonComponentPart("Addersgall1", "Addersgall 1", 27),
                                new AddonComponentPart("Addersgall2", "Addersgall 2", 28),
                                new AddonComponentPart("Addersgall3", "Addersgall 3", 29),
                                new AddonComponentPart("Addersting1", "Addersting 1", 31),
                                new AddonComponentPart("Addersting2", "Addersting 2", 32),
                                new AddonComponentPart("Addersting3", "Addersting 3", 33),
                            }
                        },
                        {
                            "JobHudGFF0",
                            new[]
                            {
                                new AddonComponentPart("Eukrasia", "Eukrasia Icon", 17),
                            }
                        }
                    }
                }
            }
        };
        
        _map = new ReadOnlyDictionary<uint, JobGaugeMap>(map);
        return _map;
    }
}

