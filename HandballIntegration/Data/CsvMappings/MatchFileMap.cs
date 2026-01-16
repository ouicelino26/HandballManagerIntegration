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
        Map(m => m.TeamScore1).Name("Score A");
        Map(m => m.TeamScore2).Name("Score B");
        Map(m => m.EventId).Name("Evenement");
        Map(m => m.ShootShade).Name("Secteur du Tir");
        Map(m => m.AttackId).Name("Type d'attaque");
        Map(m => m.DefenseId).Name("Le type de défense");
        Map(m => m.Trigger).Name("Déclenchement");
        Map(m => m.TeamId).Name("Nom de l'équipe");
        }
    }
}
