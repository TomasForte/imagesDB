from selenium import webdriver
from selenium.webdriver.firefox.options import Options
from selenium.webdriver.common.by import By
import time
import re
import requests

# Replace with your actual API token
API_TOKEN = "mytoken"

headers = {
    "Authorization": f"Bearer {API_TOKEN}"
}


# Optional: run headless
options = Options()
options = webdriver.FirefoxOptions()
profile = webdriver.FirefoxProfile("C:/Users/Utilizador/AppData/Roaming/Mozilla/Firefox/Profiles/hlvbsfje.default-release")
options.profile = profile
driver = webdriver.Firefox(options=options)


driver.get("https://imgchest.com/profile")  # Replace with your actual URL

# Wait for dynamic content to load
time.sleep(5)

# Find all matching divs
post_cards = driver.find_elements(By.CSS_SELECTOR, "div.relative.select-none.post-card")
content = driver.page_source
# Loop through and extract href from <a> tags
start_time = time.time()
for card in post_cards:
    try:
        link = card.find_element(By.TAG_NAME, "a")
        href = link.get_attribute("href")
        match = re.search(r'/p/([a-zA-Z0-9]+)', href)

        if match:
            post_id = match.group(1)
            elapsed = time.time() - start_time
            sleep_time = max(0, 1 - elapsed)
            time.sleep(sleep_time)
            response = requests.delete(f"https://api.imgchest.com/v1/post/{post_id}", headers=headers)
            start_time = time.time()
            if response.status_code == 200:
                print("Post deleted successfully.")
            else:
                print(f"Failed to delete post: {response.status_code}")
                print(response.text)



    except:
        print("No <a> tag found in this card.")

driver.quit()