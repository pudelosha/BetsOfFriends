import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyBetsToPlacePage } from './my-bets-to-place.page';

describe('MyBetsToPlacePage', () => {
  let component: MyBetsToPlacePage;
  let fixture: ComponentFixture<MyBetsToPlacePage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(MyBetsToPlacePage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
