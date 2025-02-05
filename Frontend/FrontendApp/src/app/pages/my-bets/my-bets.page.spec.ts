import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyBetsPage } from './my-bets.page';

describe('MyBetsPage', () => {
  let component: MyBetsPage;
  let fixture: ComponentFixture<MyBetsPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(MyBetsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
