import logging
import os
import requests

type Url = str

AVAILABLE_MODS: dict[str, Url] = {
    "Example": "https://github.com/guigui0246/ToopleBitMod/releases/download/v0.1.0/ToppleBitMod.dll",
    "ToppleBitMod": "https://github.com/guigui0246/ToopleBitMod/releases/download/v0.1.0/ToppleBitMod.dll",
    "Reset Domino": "https://github.com/guigui0246/ToopleBitMod/releases/download/v0.1.0/ToppleBitMod.dll",
}


def update_mods(mod_list: list[str], game_install_path: str) -> None:

    mods_dir = os.path.join(game_install_path, "Mods")
    os.makedirs(mods_dir, exist_ok=True)

    for mod_name in mod_list:
        if mod_name in AVAILABLE_MODS:
            url = AVAILABLE_MODS[mod_name]
            response = requests.get(url)
            response.raise_for_status()

            mod_path = os.path.join(mods_dir, f"{mod_name}.dll")
            with open(mod_path, "wb") as mod_file:
                mod_file.write(response.content)
            logging.info(f"Installed/Updated mod: {mod_name}")
        else:
            logging.warning(f"Mod '{mod_name}' not found in available mods.")

    for existing_mod in os.listdir(mods_dir):
        mod_name, ext = os.path.splitext(existing_mod)
        # Remove available mods that are not in the mod_list
        if ext == ".dll" and mod_name not in mod_list and mod_name in AVAILABLE_MODS:
            os.remove(os.path.join(mods_dir, existing_mod))
            logging.info(f"Removed mod: {mod_name}")


__all__ = ["AVAILABLE_MODS", "update_mods"]
