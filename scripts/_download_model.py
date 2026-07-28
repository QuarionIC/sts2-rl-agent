"""Resilient GGUF download: fresh client per attempt, resumes on failure."""
import sys, time, subprocess
REPO, FILE = "unsloth/Qwen3.6-27B-GGUF", "Qwen3.6-27B-Q3_K_M.gguf"
for attempt in range(40):
    # New process each attempt: hf's httpx client cannot be reused after
    # a forcibly-closed connection ("client has been closed").
    r = subprocess.run([sys.executable, "-c",
        f"from huggingface_hub import hf_hub_download;"
        f"print(hf_hub_download({REPO!r},{FILE!r},local_dir='models'))"],
        capture_output=True, text=True)
    if r.returncode == 0:
        print("DONE:", r.stdout.strip(), flush=True); break
    print(f"attempt {attempt+1}: {r.stderr.strip()[-160:]}", flush=True)
    time.sleep(15)
