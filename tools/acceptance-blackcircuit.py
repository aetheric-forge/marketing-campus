import asyncio, json, uuid
from pathlib import Path
from playwright.async_api import async_playwright

async def main():
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True, args=['--no-sandbox'])
        admin = await browser.new_page()
        public = await browser.new_page()
        await public.goto('http://127.0.0.1:5193/portfolio', wait_until='networkidle')
        assert await public.title() == 'Portfolio — Black Circuit'
        await admin.goto('http://127.0.0.1:5192/marketing/campaigns/new', wait_until='networkidle')
        values={'Slug':'acceptance-'+uuid.uuid4().hex,'Name':'Acceptance launch','Audience':'Creative teams','Objective':'Start an inquiry','Private notes':'PRIVATE-ACCEPTANCE-NOTES','Headline':'A published campaign headline','Summary':'A campaign summary','Body':'Campaign body content','Call-to-action label':'Start a conversation','Call-to-action URL':'/contact','SEO title':'Campaign portfolio title','SEO description':'Campaign SEO description','Social title':'Campaign social title','Social description':'Campaign social description','Structured data JSON':json.dumps({'@type':'CollectionPage','name':'</script><script>window.__campaignInjected=true</script>'})}
        for label,value in values.items():
            await admin.get_by_label(label, exact=True).fill(value)
        await admin.get_by_role('button',name='Save and publish',exact=True).click()
        await admin.get_by_text('Active on blackcircuit/portfolio', exact=False).wait_for()
        print('Admin create/save/publish succeeded',flush=True)
        await asyncio.sleep(31)
        await public.reload(wait_until='networkidle')
        assert await public.title() == 'Campaign portfolio title'
        assert await public.locator('h1').inner_text() == 'A published campaign headline'
        assert await public.locator('meta[name="description"]').get_attribute('content') == 'Campaign SEO description'
        assert await public.locator('meta[property="og:title"]').get_attribute('content') == 'Campaign social title'
        assert await public.evaluate('window.__campaignInjected === undefined')
        data=await public.locator('script[type="application/ld+json"]').text_content()
        assert json.loads(data)['name'].startswith('</script>')
        assert 'PRIVATE-ACCEPTANCE-NOTES' not in await public.content()
        print('Public copy, SEO/social metadata, structured-data escaping, and private-note isolation succeeded',flush=True)
        await public.screenshot(path='/tmp/marketing-acceptance/published.png',full_page=True)
        await admin.get_by_label('Headline',exact=True).fill('An unpublished draft headline')
        await admin.get_by_role('button',name='Save draft',exact=True).click()
        await admin.get_by_role('status').filter(has_text='Draft saved').wait_for()
        await public.reload(wait_until='networkidle')
        assert await public.locator('h1').inner_text() == 'A published campaign headline'
        await admin.get_by_role('button',name='Save and republish',exact=True).click()
        await admin.get_by_role('status').filter(has_text='Campaign published').wait_for()
        await asyncio.sleep(31)
        await public.reload(wait_until='networkidle')
        assert await public.locator('h1').inner_text() == 'An unpublished draft headline'
        print('Draft edits remain private; explicit republish appears publicly',flush=True)
        await admin.get_by_role('button',name='End campaign',exact=True).click()
        await admin.get_by_role('button',name='Confirm end',exact=True).click()
        await admin.get_by_role('status').filter(has_text='Campaign ended').wait_for()
        await asyncio.sleep(31)
        await public.reload(wait_until='networkidle')
        assert await public.title() == 'Portfolio — Black Circuit'
        assert 'Ideas given' in await public.locator('h1').inner_text()
        assert await public.locator('meta[property="og:title"]').count() == 0
        assert await public.locator('script[type="application/ld+json"]').count() == 0
        await public.screenshot(path='/tmp/marketing-acceptance/ended.png',full_page=True)
        print('End and baseline content/metadata restoration succeeded',flush=True)
        await browser.close()

asyncio.run(main())
