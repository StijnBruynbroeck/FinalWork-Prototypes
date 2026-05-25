# Servers opstarten voor ApiTest

Voor dit project moeten **2 lokale servers** draaien:

## 1. Ollama (AI LLM)

```powershell
ollama serve
```

- Draait op `http://localhost:11434`
- Model: `llama3` (eenmalig downloaden met `ollama pull llama3`)

## 2. Whisper (spraakherkenning)

```powershell
C:\whisper\Release\whisper-server.exe --port 9090 --model C:\whisper\Release\ggml-base.bin
```

- Draait op `http://localhost:9090/inference`
- Model: `ggml-base.bin` (bevindt zich in `C:\whisper\Release\`)

## Controleren of ze draaien

```powershell
netstat -an | Select-String "LISTEN" | Select-String "11434|9090"
```

Beide poorten moeten `LISTENING` tonen.

## Dan

Open de `ApiTest` map in Unity en klik op **Play**. Houd **T** ingedrukt om te praten.
