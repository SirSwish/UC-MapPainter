from PIL import Image
import os

def resize_image(input_path, output_path, size=(450, 450)):
    # Open the image
    with Image.open(input_path) as img:
        # Resize the image
        resized_img = img.resize(size, Image.LANCZOS)
        # Save the resized image
        resized_img.save(output_path)

def process_images_in_directory(directory, size=(450, 450)):
    # Create an output directory if it doesn't exist
    output_directory = os.path.join(directory, 'resized')
    if not os.path.exists(output_directory):
        os.makedirs(output_directory)

    # Process each image in the directory
    for filename in os.listdir(directory):
        if filename.lower().endswith('.png'):
            input_path = os.path.join(directory, filename)
            output_path = os.path.join(output_directory, filename)
            resize_image(input_path, output_path, size)
            print(f"Resized and saved: {output_path}")

# Get the current working directory
current_directory = os.getcwd()

# Process images in the current directory
process_images_in_directory(current_directory)
