from .inputs_to_outputs import generate_pngs
from .raw_file_generator import generate_raw_file

def run_pipeline():
    generate_raw_file()
    generate_pngs()