using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace PickemsPlanter.Extensions
{
	public static class ConfigurationBuilderExtensions
	{
		public static void AddKeyVault(this IConfigurationBuilder builder, IConfigurationRoot config)
		{
			string? keyVaultURI = config["KeyVault:URL"];

			if (keyVaultURI is not null)
				builder.AddAzureKeyVault(new SecretClient(new Uri(keyVaultURI), new DefaultAzureCredential()), new KeyVaultSecretManager());
		}
	}
}
