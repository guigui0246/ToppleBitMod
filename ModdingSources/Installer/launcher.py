from options import Options


def launcher(options: Options):
    """Launch the installer with the given options."""
    print("Launching installer with options:")
    print(f"Game install path: {options.game_install_path}")
    print(f"Mod list: {options.mod_list}")
