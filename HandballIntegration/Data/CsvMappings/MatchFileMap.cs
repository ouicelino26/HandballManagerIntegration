using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandballManagerCore.DTO;
using CsvHelper.Configuration;

namespace HanballManagerMaui.Services.CsvMappings
{
    public sealed class MatchFileMap :ClassMap<MatchFileDto>
    {
        public MatchFileMap() { 
        Map(m=> m.PlayerId).Name("Nom Prénom");
        Map(m => m.Time).Name("Temps");
        Map(m => m.Number).Name("N° maillot");
        Map(m => m.MiTemps)
            .Name("Mitemps", "Mi-temps", "Mi temps", "Mi_temps", "MT", "MiTemps")
            .Optional();
        Map(m => m.TeamScore1).Name("Score A");
        Map(m => m.TeamScore2).Name("Score B");
        Map(m => m.EventId).Name("Evenement");
        Map(m => m.Action)
            .Name("Complément d'action", "Complement d'action", "Action")
            .Optional();
        Map(m => m.ShootZone)
            .Name("Zone départ", "Zone depart")
            .Optional();
        Map(m => m.Shade).Name("Forme").Optional();
        Map(m => m.ShootShade).Name("Secteur du Tir");
        Map(m => m.ArmSide)
            .Convert(args =>
            {
                if (args.Row.TryGetField("Bras tireur", out string? armSide)
                    && !string.IsNullOrWhiteSpace(armSide))
                {
                    return armSide;
                }

                if (args.Row.TryGetField("D ou G", out armSide)
                    && !string.IsNullOrWhiteSpace(armSide))
                {
                    return armSide;
                }

                return null;
            });
        Map(m => m.Jump).Name("Saut").Optional();
        Map(m => m.AttackId).Name("Type d'attaque").Optional();
        Map(m => m.DefenseId).Name("Le type de défense").Optional();
        Map(m => m.Trigger).Name("Déclenchement").Optional();
        Map(m => m.PlayerNumber1)
            .Name("Présence Joueur A", "Presence Joueur A")
            .Optional();
        Map(m => m.PlayerNumber2)
            .Name("Présence Joueur B", "Presence Joueur B")
            .Optional();
        Map(m => m.TeamId).Name("Nom de l'équipe");
        }
    }
}
