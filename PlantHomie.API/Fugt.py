import time
import explorerhat

VOLT_REF = 3.3     # reference-spænding (3.3 V)
INTERVAL = 2       # sekunder mellem aflæsninger

try:
    while True:
        # explorerhat.analog.one.read() giver et flydende tal 0.0–1.0
        ratio   = explorerhat.analog.one.read()
        voltage = ratio * VOLT_REF
        pct     = ratio * 100
        print(f"Spænding: {voltage:.2f} V → Jordfugt: {pct:.1f} %")
        time.sleep(INTERVAL)
except KeyboardInterrupt:
    print("\nTest afbrudt.")
