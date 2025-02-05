import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyBetsFinalisedPage } from './my-bets-finalised.page';

describe('MyBetsFinalisedPage', () => {
  let component: MyBetsFinalisedPage;
  let fixture: ComponentFixture<MyBetsFinalisedPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(MyBetsFinalisedPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
