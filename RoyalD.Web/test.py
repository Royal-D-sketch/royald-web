from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import Select
import time

options = webdriver.ChromeOptions()
options.add_argument('--headless')
options.add_argument('--window-size=1920,1080')
driver = webdriver.Chrome(options=options)

try:
    driver.get('http://localhost:5165/Debtor/Detail/4007')
    time.sleep(1)
    
    # Login
    driver.find_element(By.NAME, 'username').send_keys('admin')
    driver.find_element(By.NAME, 'password').send_keys('admin')
    driver.find_element(By.CSS_SELECTOR, 'button[type=""submit""]').click()
    time.sleep(2)
    
    # Select WaitingGoods
    select = Select(driver.find_element(By.ID, 'statusSelect'))
    select.select_by_value('WaitingGoods')
    time.sleep(1)
    
    # Take screenshot
    driver.save_screenshot('screenshot.png')
    print('Screenshot saved.')
    
    # Check if f-waiting is visible
    f_waiting = driver.find_element(By.ID, 'f-waiting')
    print('f-waiting displayed:', f_waiting.is_displayed())
    print('f-waiting html:', f_waiting.get_attribute('outerHTML'))
finally:
    driver.quit()
