#!/usr/bin/env python3
"""
Verify that the environment is properly configured for the research paper agent.

This script checks that all required dependencies are installed and configured.
"""

import sys
from pathlib import Path


def check_python_version():
    """Verify Python version is 3.12 or higher."""
    version = sys.version_info
    if version.major < 3 or (version.major == 3 and version.minor < 12):
        print(f"❌ Python 3.12+ required (found {version.major}.{version.minor})")
        return False

    print(f"✓ Python {version.major}.{version.minor} found")
    return True


def check_dependencies():
    """Verify all required packages are installed."""
    required = ["autogen", "mistralai", "requests"]
    missing = []

    for pkg in required:
        try:
            __import__(pkg)
            print(f"✓ {pkg} installed")
        except ImportError:
            print(f"❌ {pkg} not found")
            missing.append(pkg)

    if missing:
        print(f"\nInstall missing packages with:")
        print(f"pip install {' '.join(missing)}")

    return len(missing) == 0


def check_env_file():
    """Check that .env file exists and has required variables."""
    env_file = Path(".env")

    if not env_file.exists():
        print("❌ .env file not found")
        print("   Create one with: cp .env.example .env")
        print("   Then add your MISTRAL_API_KEY")
        return False

    # Check for MISTRAL_API_KEY
    with open(env_file) as f:
        content = f.read()

    if "MISTRAL_API_KEY" not in content:
        print("❌ MISTRAL_API_KEY not found in .env")
        return False

    if "your_mistral_api_key" in content.lower():
        print("❌ MISTRAL_API_KEY not configured (still has placeholder)")
        return False

    print("✓ .env file configured")
    return True


def check_project_files():
    """Verify all required project files exist."""
    required_files = [
        "main.py",
        "tools.py",
        "config.py",
        "evaluation.py",
        "utils.py",
        "requirements.txt",
        "README.md",
    ]

    all_exist = True
    for fname in required_files:
        path = Path(fname)
        if path.exists():
            print(f"✓ {fname} found")
        else:
            print(f"❌ {fname} missing")
            all_exist = False

    return all_exist


def main():
    print("=" * 70)
    print("Research Paper Search Agent - Setup Verification")
    print("=" * 70 + "\n")

    checks = [
        ("Python Version", check_python_version),
        ("Dependencies", check_dependencies),
        ("Environment File", check_env_file),
        ("Project Files", check_project_files),
    ]

    results = []
    for name, check_fn in checks:
        print(f"\nChecking {name}...")
        try:
            result = check_fn()
            results.append((name, result))
        except Exception as e:
            print(f"❌ Error during check: {e}")
            results.append((name, False))

    print("\n" + "=" * 70)
    print("Setup Summary")
    print("=" * 70)

    all_passed = all(result for _, result in results)

    for name, result in results:
        status = "✓ PASS" if result else "❌ FAIL"
        print(f"{status}: {name}")

    print("=" * 70)

    if all_passed:
        print("\n✓ Setup is complete! You can now run:")
        print("  python main.py        (interactive mode)")
        print("  python demo.py         (run demo)")
        print("  python run_evaluation.py  (evaluate agent)")
        return 0
    else:
        print("\n❌ Please fix the issues above before running the agent.")
        return 1


if __name__ == "__main__":
    sys.exit(main())
