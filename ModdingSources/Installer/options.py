import argparse
from typing import Any

import yaml


class Options:
    game_install_path: str | None  # Path to the game installation directory
    mod_list: list[str]  # List of mods to install
    no_window: bool  # Wether to run the installer without a GUI
    auto_run: bool  # Wether to auto-run the game after installation
    start_on_startup: bool  # Wether to start the installer on system startup
    auto_update_mods: bool  # Wether to auto-update installed mods
    auto_update_installer: bool  # Wether to auto-update the installer itself
    auto_update_game: bool  # Wether to auto-update the game
    backup_before_install: bool  # Wether to backup game files before installing mods
    restore_backup_on_failure: (
        bool  # Wether to restore from backup if installation fails
    )
    setting_save_path: str | None  # Path to save the installer settings
    installer_install_path: (
        str | None
    )  # Path where the installer will be installed in case it isn't installed and freshly downloaded


def parse_options():
    parser = argparse.ArgumentParser(description="Installer Options")
    parser.add_argument(
        "--game_install_path",
        type=str,
        help="Path to the game installation directory",
        default=None,
    )
    parser.add_argument(
        "--mod_list", type=str, nargs="*", help="List of mods to install", default=[]
    )
    parser.add_argument(
        "--no_window",
        action="store_true",
        help="Run the installer without a GUI",
        default=False,
    )
    parser.add_argument(
        "--auto_run",
        action="store_true",
        help="Auto-run the game after installation",
        default=False,
    )
    parser.add_argument(
        "--start_on_startup",
        action="store_true",
        help="Start the installer on system startup",
        default=False,
    )
    parser.add_argument(
        "--auto_update_mods",
        action="store_true",
        help="Auto-update installed mods",
        default=False,
    )
    parser.add_argument(
        "--auto_update_installer",
        action="store_true",
        help="Auto-update the installer itself",
        default=False,
    )
    parser.add_argument(
        "--auto_update_game",
        action="store_true",
        help="Auto-update the game",
        default=False,
    )
    parser.add_argument(
        "--backup_before_install",
        action="store_true",
        help="Backup game files before installing mods",
        default=False,
    )
    parser.add_argument(
        "--restore_backup_on_failure",
        action="store_true",
        help="Restore from backup if installation fails",
        default=False,
    )
    parser.add_argument(
        "--setting_save_path",
        type=str,
        help="Path to save the installer settings",
        default=None,
    )
    parser.add_argument(
        "--installer_install_path",
        type=str,
        help="Path where the installer will be installed",
        default=None,
    )

    # Parse the arguments
    args = parser.parse_args()

    # Create an Options instance with default values if not provided
    options = Options()
    options.game_install_path = args.game_install_path
    options.mod_list = args.mod_list
    options.no_window = args.no_window
    options.auto_run = args.auto_run
    options.start_on_startup = args.start_on_startup
    options.auto_update_mods = args.auto_update_mods
    options.auto_update_installer = args.auto_update_installer
    options.auto_update_game = args.auto_update_game
    options.backup_before_install = args.backup_before_install
    options.restore_backup_on_failure = args.restore_backup_on_failure
    options.setting_save_path = args.setting_save_path
    options.installer_install_path = args.installer_install_path

    return options


def load_config(file_path: str) -> Any:
    with open(file_path, "r") as file:
        return yaml.safe_load(file)


def save_config(file_path: str, config: Any):
    with open(file_path, "w") as file:
        yaml.safe_dump(config, file)


def save_options(options: Options):

    assert options.setting_save_path is not None, "Setting save path must be provided to save options."

    config_to_save = {
        key: getattr(options, key)
        for key in vars(options)
        if getattr(options, key) is not None
        and getattr(options, key) != []
        and getattr(options, key) is not False
    }
    save_config(options.setting_save_path, config_to_save)


__all__ = ["Options", "parse_options", "load_config", "save_options"]
