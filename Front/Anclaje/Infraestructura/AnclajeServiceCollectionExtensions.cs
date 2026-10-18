namespace Front.Anclaje;

public static class AnclajeServiceCollectionExtensions
{
    /// <summary>En Program.cs: <c>builder.Services.AddAnclajeAttest(builder.Configuration);</c></summary>
    public static IServiceCollection AddAnclajeAttest(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<AnclajeOptions>()
            .Bind(config.GetSection(AnclajeOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Anclaje:ApiKey es obligatoria (user-secrets o variable de entorno).")
            .Validate(o => o.DireccionEmisora.StartsWith('G'), "Anclaje:DireccionEmisora debe ser una dirección G…")
            .Validate(o => o.SchemaUid.Length == 64, "Anclaje:SchemaUid debe tener 64 caracteres hexadecimales.")
            .ValidateOnStart();

        services.AddHttpClient<IAnclajeEvidencia, AttestAnclajeAdapter>((sp, http) =>
        {
            var o = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AnclajeOptions>>().Value;
            http.BaseAddress = new Uri(o.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(o.TimeoutSegundos);
            http.DefaultRequestHeaders.Add("X-Bridge-Key", o.ApiKey);
        });
        return services;
    }
}
