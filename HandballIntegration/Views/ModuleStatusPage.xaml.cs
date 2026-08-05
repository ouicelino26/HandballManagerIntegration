using System.Windows.Controls;
using HandballIntegration.Admin.Models;

namespace HandballIntegration.Views;

public partial class ModuleStatusPage : Page
{
    public ModuleStatusPage(AdminNavigationItem module)
    {
        InitializeComponent();
        DataContext = module;
        AvailabilityText.Text = module.IsAvailable
            ? "La fondation de ce module est raccordee. Les operations disponibles restent protegees par l'API."
            : "Ce module est visible car votre session y est autorisee, mais son interface metier n'est pas encore livree. Aucun traitement n'est simule.";
    }
}
