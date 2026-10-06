"""Installer round-trip checks using synthetic archives, never distributed game data.
Run on Windows: set KR6_LUA51_DLL to your local game's lua51.dll, then
python -m unittest discover -s tests -v
"""
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[1]

@unittest.skipUnless(os.environ.get('KR6_LUA51_DLL'), 'Provide your local lua51.dll using KR6_LUA51_DLL')
class InstallerTests(unittest.TestCase):
    def test_round_trip(self):
        with tempfile.TemporaryDirectory() as temp:
            game_dir = Path(temp)
            executable = game_dir / 'Kingdom Rush Genesis.exe'
            prefix = b'MZ' + bytes(254)
            executable.write_bytes(prefix)
            original_entries = {
                'main.lua': b'return {}',
                'all/components.lua': b'synthetic components fixture',
                'kr6/templates_game.lua': b'synthetic templates fixture',
                'kr6/data/balance/balance.lua': b'synthetic balance fixture',
                'fixture.txt': b'unchanged fixture',
            }
            with zipfile.ZipFile(executable, 'a', compression=zipfile.ZIP_DEFLATED) as archive:
                for name, value in original_entries.items():
                    archive.writestr(name, value)
            original = executable.read_bytes()
            shutil.copy2(os.environ['KR6_LUA51_DLL'], game_dir / 'lua51.dll')
            command = [sys.executable, str(ROOT / 'install.py'), '--game-dir', str(game_dir)]
            for _ in range(2):
                subprocess.run(command, check=True, capture_output=True)
                with zipfile.ZipFile(executable) as archive:
                    self.assertEqual(archive.read('codex_trainer/main_original.luac'), original_entries['main.lua'])
                    self.assertEqual(archive.read('codex_trainer/bridge.lua'), (ROOT / 'src/bridge.lua').read_text(encoding='utf-8-sig').encode('utf-8'))
                    for name, value in original_entries.items():
                        if name != 'main.lua':
                            self.assertEqual(archive.read(name), value)
                self.assertEqual(executable.read_bytes()[:len(prefix)], prefix)
            self.assertEqual((game_dir / '.kr6-trainer-backup/Kingdom Rush Genesis.original.exe').read_bytes(), original)
            subprocess.run(command + ['--uninstall'], check=True, capture_output=True)
            self.assertEqual(executable.read_bytes(), original)

if __name__ == '__main__':
    unittest.main()
