import shutil
import tempfile
import requests
from options import Options
from constants import INSTALLER_DOWNLOAD_URL, GAME_DOWNLOAD_URL, MOD_LOADER_DOWNLOAD_URL
import os
import zipfile
import logging


def launcher(options: Options):
    """Launch the installer with the given options."""

    assert options.installer_install_path is not None, "Installer install path must be set."
    assert options.game_install_path is not None, "Game install path must be set."
    assert options.setting_save_path is not None, "Setting save path must be set."
    assert options.mod_list is not None, "Mod list must be set."
    options.game_install_path = os.path.dirname(os.path.abspath(options.game_install_path))

    try:
        if options.auto_update_installer:
            logging.info("Auto-updating installer...")
            response = requests.get(INSTALLER_DOWNLOAD_URL)
            response.raise_for_status()
            with open(
                os.path.join(
                    options.installer_install_path, "ToppleBitModdingLauncher.exe"
                ),
                "wb",
            ) as f:
                f.write(response.content)

        if options.backup_before_install:
            logging.info("Backing up game files...")
            backup_dir = os.path.join(
                options.game_install_path, "backup"
            )
            tempfile_dir = tempfile.TemporaryDirectory()
            zip_file = zipfile.ZipFile(
                os.path.join(tempfile_dir.name, "game_backup.zip"), "w"
            )
            for foldername, _, filenames in os.walk(options.game_install_path):
                for filename in filenames:
                    file_path = os.path.join(foldername, filename)
                    relative_path = os.path.relpath(
                        file_path, options.game_install_path
                    )
                    zip_file.write(file_path, relative_path)
            zip_file.close()
            os.makedirs(backup_dir, exist_ok=True)
            try:
                shutil.move(
                    os.path.join(tempfile_dir.name, "game_backup.zip"), backup_dir
                )
            except shutil.Error:
                logging.info("Backup file already exists. Keeping existing backup.")

        if options.auto_update_game:
            logging.info("Auto-updating game...")
            response = requests.get(GAME_DOWNLOAD_URL, stream=True)
            response.raise_for_status()
            tempfile_dir = tempfile.TemporaryDirectory()
            game_zip_path = os.path.join(
                tempfile_dir.name, "LatestGameBuild.zip"
            )

            with open(game_zip_path, "wb") as f:
                for chunk in response.iter_content(chunk_size=8192):
                    f.write(chunk)
            with zipfile.ZipFile(game_zip_path, 'r') as zip_ref:
                members = [m for m in zip_ref.namelist() if not m.endswith('/')]
                split_paths = [m.split('/') for m in members]
                top_levels = {p[0] for p in split_paths}

                # If there's a single top-level directory, then it's double-wrapped
                if len(top_levels) == 1:
                    for m, parts in zip(members, split_paths):
                        relative_path = os.path.join(*parts[1:])
                        dest = os.path.join(options.game_install_path, relative_path)

                        os.makedirs(os.path.dirname(dest), exist_ok=True)
                        with zip_ref.open(m) as src, open(dest, 'wb') as dst:
                            dst.write(src.read())
                else:
                    zip_ref.extractall(options.game_install_path)
            tempfile_dir.cleanup()

        if options.auto_update_game or options.auto_update_installer:
            # Now we update the mod loader
            response = requests.get(MOD_LOADER_DOWNLOAD_URL, stream=True)
            response.raise_for_status()
            tempfile_dir = tempfile.TemporaryDirectory()
            mod_loader_zip_path = os.path.join(
                tempfile_dir.name, "LatestModLoaderBuild.zip"
            )

            with open(mod_loader_zip_path, "wb") as f:
                for chunk in response.iter_content(chunk_size=8192):
                    f.write(chunk)
            with zipfile.ZipFile(mod_loader_zip_path, 'r') as zip_ref:
                members = [m for m in zip_ref.namelist() if not m.endswith('/')]
                split_paths = [m.split('/') for m in members]
                top_levels = {p[0] for p in split_paths}

                # If there's a single top-level directory, then it's double-wrapped
                if len(top_levels) == 1:
                    for m, parts in zip(members, split_paths):
                        relative_path = os.path.join(*parts[1:])
                        dest = os.path.join(options.game_install_path, relative_path)

                        os.makedirs(os.path.dirname(dest), exist_ok=True)
                        with zip_ref.open(m) as src, open(dest, 'wb') as dst:
                            dst.write(src.read())
                else:
                    zip_ref.extractall(options.game_install_path)
            tempfile_dir.cleanup()

    except Exception as e:
        logging.error(f"An error occurred: {e}")
        if options.restore_backup_on_failure:
            logging.info("Restoring from backup...")
            backup_zip_path = os.path.join(
                options.game_install_path, "backup", "game_backup.zip"
            )
            if os.path.exists(backup_zip_path):
                with zipfile.ZipFile(backup_zip_path, 'r') as zip_ref:
                    zip_ref.extractall(options.game_install_path)
                logging.info("Restoration complete.")
            else:
                logging.info("No backup found to restore.")
        raise

    logging.info("Launching installer with options:")
    logging.info(f"Game install path: {options.game_install_path}")
    logging.info(f"Mod list: {options.mod_list}")
