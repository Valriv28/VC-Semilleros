// Crea una cuenta de TESTNET nueva y la fondea con Friendbot. Solo para pruebas: NUNCA para mainnet.
// Uso:  npm run crear-cuenta
import { Keypair } from "@stellar/stellar-sdk";

const kp = Keypair.random();

let r;
try {
  r = await fetch(`https://friendbot.stellar.org/?addr=${encodeURIComponent(kp.publicKey())}`);
} catch (e) {
  console.error("No se pudo contactar a Friendbot (¿sin internet o bloqueado?):", e.message ?? e);
  process.exit(1);
}
if (!r.ok) {
  console.error(`Friendbot respondió ${r.status}. Espera un minuto y vuelve a correr el comando.`);
  process.exit(1);
}

console.log("Cuenta creada y fondeada en TESTNET.\n");
console.log("Dirección pública (G…)  →  appsettings.Development.json, campo Anclaje:DireccionEmisora");
console.log(kp.publicKey());
console.log("\nLlave secreta (S…)  →  SOLO en tu .env como PLATFORM_SECRET. No la compartas, no la subas a git.");
console.log(kp.secret());
console.log("\nGuarda las dos en tu gestor de contraseñas ahora: la secreta no se puede recuperar después.");
