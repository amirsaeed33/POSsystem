import { SmartPosTemplatePage } from './app.po';

describe('SmartPos App', function() {
  let page: SmartPosTemplatePage;

  beforeEach(() => {
    page = new SmartPosTemplatePage();
  });

  it('should display message saying app works', () => {
    page.navigateTo();
    expect(page.getParagraphText()).toEqual('app works!');
  });
});
