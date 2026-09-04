using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace NetPulseOverlay.Views;

/// <summary>
/// About window for NetSpeed Live. All identity values are read from the
/// assembly so they always match the build; string fallbacks keep it working
/// even if a metadata attribute is missing.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        Assembly assembly = typeof(AboutWindow).Assembly;
        ProductNameText.Text = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "NetSpeed Live";
        VersionText.Text = "Version " + (assembly.GetName().Version?.ToString(3) ?? "1.1.0");
        CompanyText.Text = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "KSA AI STUDIO";
        CopyrightText.Text = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "© 2026 KSA AI STUDIO";

        string founder = "Ashutosh Praharaj";
        string contact = "ksaaistudio@outlook.com";
        foreach (CustomAttributeData attr in assembly.GetCustomAttributesData())
        {
            if (attr.AttributeType.Name == "AssemblyMetadataAttribute" && attr.ConstructorArguments.Count == 2)
            {
                string? key = attr.ConstructorArguments[0].Value as string;
                string? value = attr.ConstructorArguments[1].Value as string;
                if (string.Equals(key, "Founder", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                {
                    founder = value;
                }
                else if (string.Equals(key, "Contact", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                {
                    contact = value;
                }
            }
        }

        FounderText.Text = "Founder: " + founder;
        ContactText.Text = "Contact: " + contact;

        DonateButton.Click += (_, _) =>
        {
            DonationWindow donate = new DonationWindow { Owner = this };
            donate.ShowDialog();
        };
    }
}